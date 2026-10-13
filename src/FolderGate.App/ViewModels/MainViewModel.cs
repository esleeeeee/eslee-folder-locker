using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Data;
using FolderGate.App.Controls;
using FolderGate.App.Services;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Operations;
using FolderGate.Core.Security;
using FolderGate.Core.Storage;
using FolderGate.Core.Validation;

namespace FolderGate.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly AppPaths _paths;
    private readonly ConfigStore _configStore;
    private readonly PasswordService _passwordService = new();
    private readonly TargetPathValidator _pathValidator;
    private readonly IUserInteractionService _interaction;
    private readonly ElevatedToolRunner _toolRunner;
    private readonly ExplorerContextMenuService _explorerContextMenu;
    private readonly StartupRelockService _startupRelockService;
    private readonly OperationProgressStore _progressStore;
    private readonly MasterCredentialManager _masterManager;
    private FolderGateConfig _config;
    private FolderItemViewModel? _selectedFolder;
    private LockMode _selectedMode = LockMode.Quick;
    private string _statusMessage = AppText.Ready;
    private bool _isBusy;
    private string? _activeOperationId;
    private int _operationTotal;
    private int _operationCompleted;
    private int _operationFailed;
    private string _operationCurrentPath = "-";
    private string _operationElapsed = "-";
    private string _operationEta = "-";
    private string _operationPhase = "-";
    private double _operationProgressPercent;
    private bool _isProgressIndeterminate;
    private string _searchText = string.Empty;
    private FolderFilter _filter = FolderFilter.All;

    // Folder id -> operation id of every lock/unlock this window is running
    // (one entry, or several for "lock all").
    private readonly Dictionary<string, string> _runningOperations = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset _lastSeenConfigUpdateUtc;
    private int _syncTicks;

    public MainViewModel(AppPaths paths, IUserInteractionService interaction, ElevatedToolRunner toolRunner)
    {
        FoldersView = CollectionViewSource.GetDefaultView(Folders);
        FoldersView.Filter = item => item is FolderItemViewModel folder &&
                                     folder.MatchesFilter(_filter) &&
                                     folder.MatchesSearch(_searchText);
        _paths = paths;
        _interaction = interaction;
        _toolRunner = toolRunner;
        ToolLocator toolLocator = new(paths);
        _explorerContextMenu = new ExplorerContextMenuService(paths, toolLocator);
        _startupRelockService = new StartupRelockService(paths, toolLocator);
        _configStore = new ConfigStore(paths);
        _progressStore = new OperationProgressStore(paths);
        _pathValidator = new TargetPathValidator(paths);
        _masterManager = new MasterCredentialManager(paths);
        _config = _configStore.Load();

        AddFolderCommand = new AsyncRelayCommand(AddFolderAsync);
        RemoveFolderCommand = new RelayCommand(RemoveSelectedFolder, () => SelectedFolder is not null && !IsBusy);
        LockCommand = new AsyncRelayCommand(LockSelectedFolderAsync, () =>
            SelectedFolder is { IsMissing: false, State: FolderLockState.Unlocked or FolderLockState.TemporarilyUnlocked } && !IsBusy);
        UnlockCommand = new AsyncRelayCommand(UnlockSelectedFolderAsync, () =>
            SelectedFolder is { IsMissing: false, State: FolderLockState.Locked or FolderLockState.TemporarilyUnlocked or FolderLockState.RecoveryRequired } && !IsBusy);
        LockAllCommand = new AsyncRelayCommand(LockAllAsync, () => !IsBusy && LockableFolders().Any());
        RelockNowCommand = new AsyncRelayCommand(RelockSelectedNowAsync, () => !IsBusy && SelectedFolder?.State == FolderLockState.TemporarilyUnlocked);
        ExtendUnlockCommand = new ParameterCommand(ExtendSelectedUnlock, _ => !IsBusy && SelectedFolder?.State == FolderLockState.TemporarilyUnlocked);
        SetFolderPasswordCommand = new RelayCommand(SetSelectedFolderPassword, () => SelectedFolder is not null && !IsBusy);
        ClearFolderPasswordCommand = new RelayCommand(ClearSelectedFolderPassword, () => SelectedFolder is { HasOwnPassword: true } && !IsBusy);
        ChangePasswordCommand = new RelayCommand(ChangePassword, () => !IsBusy);
        ShowLogsCommand = new RelayCommand(() => _interaction.ShowLogFile(_paths.LogFilePath));
        OpenRecoveryToolCommand = new AsyncRelayCommand(OpenRecoveryToolAsync, () => !IsBusy);
        RegisterExplorerMenuCommand = new RelayCommand(RegisterExplorerMenu, () => !IsBusy);
        UnregisterExplorerMenuCommand = new RelayCommand(UnregisterExplorerMenu, () => !IsBusy);
        CancelOperationCommand = new RelayCommand(CancelCurrentOperation, () => IsBusy && _runningOperations.Count > 0);
        ChangeMasterPasswordCommand = new RelayCommand(ChangeMasterPassword, () => !IsBusy);
        ChangeMasterHintCommand = new RelayCommand(ChangeMasterHint, () => !IsBusy);
        OpenInExplorerCommand = new RelayCommand(OpenSelectedInExplorer, () => CanOpenInExplorer);

        RefreshFolders();
    }

    public ObservableCollection<FolderItemViewModel> Folders { get; } = [];

    /// <summary>Folders filtered by <see cref="SearchText"/> and <see cref="Filter"/>.</summary>
    public ICollectionView FoldersView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
            {
                RefreshView();
            }
        }
    }

    public FolderFilter Filter
    {
        get => _filter;
        set
        {
            if (SetProperty(ref _filter, value))
            {
                OnPropertyChanged(nameof(IsFilterAll));
                OnPropertyChanged(nameof(IsFilterLocked));
                OnPropertyChanged(nameof(IsFilterUnlocked));
                OnPropertyChanged(nameof(IsFilterAttention));
                RefreshView();
            }
        }
    }

    public bool IsFilterAll
    {
        get => Filter == FolderFilter.All;
        set
        {
            if (value)
            {
                Filter = FolderFilter.All;
            }
        }
    }

    public bool IsFilterLocked
    {
        get => Filter == FolderFilter.Locked;
        set
        {
            if (value)
            {
                Filter = FolderFilter.Locked;
            }
        }
    }

    public bool IsFilterUnlocked
    {
        get => Filter == FolderFilter.Unlocked;
        set
        {
            if (value)
            {
                Filter = FolderFilter.Unlocked;
            }
        }
    }

    public bool IsFilterAttention
    {
        get => Filter == FolderFilter.Attention;
        set
        {
            if (value)
            {
                Filter = FolderFilter.Attention;
            }
        }
    }

    public int TotalCount => Folders.Count;

    public int LockedCount => Folders.Count(folder => folder.MatchesFilter(FolderFilter.Locked));

    public int UnlockedCount => Folders.Count(folder => folder.MatchesFilter(FolderFilter.Unlocked));

    public int AttentionCount => Folders.Count(folder => folder.MatchesFilter(FolderFilter.Attention));

    public bool HasAttention => AttentionCount > 0 || Filter == FolderFilter.Attention;

    public bool HasFolders => Folders.Count > 0;

    public bool HasNoMatches => HasFolders && FoldersView.IsEmpty;

    public string FolderCountText => AppText.FolderCount(Folders.Count);

    public FolderItemViewModel? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetProperty(ref _selectedFolder, value))
            {
                if (value is not null)
                {
                    SelectedMode = value.Model.Mode;
                }

                RaiseCommandStates();
                NotifySelectionChanged();
            }
        }
    }

    public bool HasSelection => SelectedFolder is not null;

    public string SelectedTitle => SelectedFolder?.DisplayName ?? string.Empty;

    public FolderLockState? SelectedStateValue => SelectedFolder?.State;

    public string SelectedStateGlyph => SelectedFolder?.StateGlyph ?? Glyphs.Folder;

    public string SelectedStatusTitle => SelectedFolder?.State switch
    {
        FolderLockState.Locked => AppText.StatusLockedTitle,
        FolderLockState.TemporarilyUnlocked => AppText.StatusTemporaryTitle,
        FolderLockState.Working => AppText.StatusWorkingTitle,
        FolderLockState.RecoveryRequired => AppText.StatusRecoveryTitle,
        FolderLockState.Unlocked => AppText.StatusUnlockedTitle,
        _ => string.Empty
    };

    public string SelectedStatusBody => SelectedFolder?.State switch
    {
        FolderLockState.Locked => AppText.StatusLockedBody,
        FolderLockState.TemporarilyUnlocked => AppText.StatusTemporaryBody(SelectedFolder.RelockTimeText),
        FolderLockState.Working => AppText.StatusWorkingBody,
        FolderLockState.RecoveryRequired => AppText.StatusRecoveryBody,
        FolderLockState.Unlocked => AppText.StatusUnlockedBody,
        _ => string.Empty
    };

    /// <summary>Lock is the main action of an unlocked folder.</summary>
    public bool IsLockPrimary => SelectedFolder?.State is FolderLockState.Unlocked;

    /// <summary>A temporary unlock ends with "lock now" (no UAC prompt) or can be extended.</summary>
    public bool IsRelockPrimary => SelectedFolder?.State is FolderLockState.TemporarilyUnlocked;

    /// <summary>Unlock is the main action of a locked folder.</summary>
    public bool IsUnlockPrimary => SelectedFolder?.State is FolderLockState.Locked;

    public bool IsRecoveryPrimary => SelectedFolder?.State is FolderLockState.RecoveryRequired;

    /// <summary>Unlocking a temporarily unlocked folder makes the unlock permanent.</summary>
    public bool ShowUnlockSecondary => SelectedFolder?.State is FolderLockState.TemporarilyUnlocked or FolderLockState.RecoveryRequired;

    public bool ShowModeChoice => SelectedFolder?.State is FolderLockState.Unlocked;

    public bool CanOpenInExplorer => SelectedFolder is { IsMissing: false, State: FolderLockState.Unlocked or FolderLockState.TemporarilyUnlocked };

    public bool IsSelectedBusy => IsBusy && SelectedFolder is not null && _runningOperations.ContainsKey(SelectedFolder.Id);

    public bool IsSelectedTemporary => SelectedFolder?.State == FolderLockState.TemporarilyUnlocked;

    public string SelectedRemainingText => SelectedFolder?.RemainingText ?? string.Empty;

    public bool SelectedIsMissing => SelectedFolder?.IsMissing == true;

    public string SelectedPresenceTitle => SelectedFolder?.Presence == FolderPresence.DriveUnavailable
        ? AppText.PresenceDriveTitle
        : AppText.PresenceMissingTitle;

    public string SelectedPresenceBody => SelectedFolder?.Presence == FolderPresence.DriveUnavailable
        ? AppText.PresenceDriveBody
        : AppText.PresenceMissingBody;

    public bool SelectedHasOwnPassword => SelectedFolder?.HasOwnPassword == true;

    public string SelectedPasswordKind => SelectedHasOwnPassword ? AppText.PasswordKindFolder : AppText.PasswordKindShared;

    public string SelectedFolderPasswordMenuText => SelectedHasOwnPassword ? AppText.FolderPasswordChangeMenu : AppText.FolderPasswordSetMenu;

    /// <summary>Recent log entries of the selected folder, newest first.</summary>
    public ObservableCollection<FolderHistoryEntry> SelectedHistory { get; } = [];

    public bool HasSelectedHistory => SelectedHistory.Count > 0;

    public string SelectedModeText => SelectedFolder?.ModeText ?? "-";

    public string SelectedLastOperationShort => SelectedFolder?.LastOperationShortText ?? "-";

    public bool SelectedHasWarning => SelectedFolder?.HasWarning == true;

    public string SelectedWarningText => SelectedFolder?.WarningText ?? string.Empty;

    public bool IsQuickMode
    {
        get => SelectedMode == LockMode.Quick;
        set
        {
            if (value)
            {
                SelectedMode = LockMode.Quick;
            }
        }
    }

    public bool IsHardenedMode
    {
        get => SelectedMode == LockMode.Hardened;
        set
        {
            if (value)
            {
                SelectedMode = LockMode.Hardened;
            }
        }
    }

    public LockMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetProperty(ref _selectedMode, value))
            {
                OnPropertyChanged(nameof(IsQuickMode));
                OnPropertyChanged(nameof(IsHardenedMode));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandStates();
                OnPropertyChanged(nameof(IsSelectedBusy));
            }
        }
    }

    public string SelectedPath => SelectedFolder?.Path ?? "-";

    public string SelectedState => SelectedFolder?.StateText ?? "-";

    public string SelectedLastOperation => SelectedFolder?.LastOperationText ?? "-";

    public string SelectedLastResult => SelectedFolder?.LastResult ?? "-";

    public int OperationTotal
    {
        get => _operationTotal;
        set => SetProperty(ref _operationTotal, value);
    }

    public int OperationCompleted
    {
        get => _operationCompleted;
        set => SetProperty(ref _operationCompleted, value);
    }

    public int OperationFailed
    {
        get => _operationFailed;
        set => SetProperty(ref _operationFailed, value);
    }

    public string OperationCurrentPath
    {
        get => _operationCurrentPath;
        set => SetProperty(ref _operationCurrentPath, value);
    }

    public string OperationElapsed
    {
        get => _operationElapsed;
        set => SetProperty(ref _operationElapsed, value);
    }

    public string OperationEta
    {
        get => _operationEta;
        set => SetProperty(ref _operationEta, value);
    }

    public string OperationPhase
    {
        get => _operationPhase;
        set => SetProperty(ref _operationPhase, value);
    }

    public double OperationProgressPercent
    {
        get => _operationProgressPercent;
        set => SetProperty(ref _operationProgressPercent, value);
    }

    public bool IsProgressIndeterminate
    {
        get => _isProgressIndeterminate;
        set => SetProperty(ref _isProgressIndeterminate, value);
    }

    public AsyncRelayCommand AddFolderCommand { get; }

    public RelayCommand RemoveFolderCommand { get; }

    public AsyncRelayCommand LockCommand { get; }

    public AsyncRelayCommand UnlockCommand { get; }

    public RelayCommand ChangePasswordCommand { get; }

    public RelayCommand ShowLogsCommand { get; }

    public AsyncRelayCommand OpenRecoveryToolCommand { get; }

    public RelayCommand RegisterExplorerMenuCommand { get; }

    public RelayCommand UnregisterExplorerMenuCommand { get; }

    public RelayCommand CancelOperationCommand { get; }

    public RelayCommand ChangeMasterPasswordCommand { get; }

    public RelayCommand ChangeMasterHintCommand { get; }

    public RelayCommand OpenInExplorerCommand { get; }

    public AsyncRelayCommand LockAllCommand { get; }

    public AsyncRelayCommand RelockNowCommand { get; }

    /// <summary>Extends a temporary unlock; the command parameter is the number of minutes.</summary>
    public ParameterCommand ExtendUnlockCommand { get; }

    public RelayCommand SetFolderPasswordCommand { get; }

    public RelayCommand ClearFolderPasswordCommand { get; }

    private async Task AddFolderAsync()
    {
        string? selectedPath = _interaction.SelectFolder();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        RegisterFolders([selectedPath]);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Registers folders dropped onto the main window. Files are skipped; every
    /// folder goes through the same validation as the folder picker.
    /// </summary>
    public void AddDroppedPaths(IReadOnlyList<string> paths)
    {
        List<string> folders = paths.Where(Directory.Exists).ToList();
        int skipped = paths.Count - folders.Count;
        if (folders.Count == 0)
        {
            if (skipped > 0)
            {
                _interaction.ShowInfo(AppText.DropIgnoredFiles(skipped));
            }

            return;
        }

        RegisterFolders(folders);
        if (skipped > 0)
        {
            StatusMessage = AppText.DropIgnoredFiles(skipped);
        }
    }

    private void RegisterFolders(IReadOnlyList<string> paths)
    {
        _config = _configStore.Load();
        List<(string Path, PathValidationResult Validation)> accepted = [];
        List<string> problems = [];
        foreach (string path in paths)
        {
            PathValidationResult validation = _pathValidator.ValidateDirectory(path);
            if (!validation.IsValid || validation.NormalizedPath is null)
            {
                problems.Add(paths.Count == 1
                    ? validation.ErrorMessage ?? AppText.InvalidFolderGeneric
                    : $"{path}: {validation.ErrorMessage ?? AppText.InvalidFolderGeneric}");
                continue;
            }

            bool duplicate = _config.Folders.Any(folder => WindowsPathComparer.AreSamePath(folder.Path, validation.NormalizedPath)) ||
                             accepted.Any(item => WindowsPathComparer.AreSamePath(item.Validation.NormalizedPath!, validation.NormalizedPath));
            if (duplicate)
            {
                problems.Add(paths.Count == 1 ? AppText.AlreadyRegisteredFolder : $"{path}: {AppText.AlreadyRegisteredFolder}");
                continue;
            }

            accepted.Add((path, validation));
        }

        if (accepted.Count == 0)
        {
            if (problems.Count == 1 && paths.Count == 1 && problems[0] == AppText.AlreadyRegisteredFolder)
            {
                _interaction.ShowInfo(problems[0]);
            }
            else if (problems.Count > 0)
            {
                _interaction.ShowError(string.Join(Environment.NewLine, problems));
            }

            return;
        }

        PasswordRecord? firstPassword = null;
        if (_config.Password is null)
        {
            string? newPassword = _interaction.AskNewPassword(AppText.NewPasswordTitle, AppText.NewPasswordMessage);
            if (newPassword is null)
            {
                return;
            }

            firstPassword = _passwordService.CreatePasswordRecord(newPassword);
        }

        string ownerSid = WindowsIdentity.GetCurrent().User?.Value ?? string.Empty;
        List<RegisteredFolder> added = [];
        foreach ((_, PathValidationResult validation) in accepted)
        {
            RegisteredFolder registeredFolder = new()
            {
                DisplayName = new DirectoryInfo(validation.NormalizedPath!).Name,
                Path = validation.NormalizedPath!,
                Mode = SelectedMode,
                State = FolderLockState.Unlocked,
                OwnerSid = ownerSid,
                HasReparsePointWarning = validation.Warnings.Count > 0,
                LastOperationUtc = DateTimeOffset.UtcNow,
                LastResult = validation.Warnings.Count > 0 ? validation.Warnings[0] : AppText.Registered
            };
            added.Add(registeredFolder);
        }

        // Save under the configuration lock; a folder registered meanwhile by another
        // window is not added twice.
        string? lastId = null;
        int addedCount = 0;
        _configStore.Update(config =>
        {
            if (config.Password is null && firstPassword is not null)
            {
                config.Password = firstPassword;
            }

            foreach (RegisteredFolder folder in added.Where(folder => !config.Folders.Any(existing => WindowsPathComparer.AreSamePath(existing.Path, folder.Path))))
            {
                config.Folders.Add(folder);
                lastId = folder.Id;
                addedCount++;
            }

            return addedCount > 0 || firstPassword is not null;
        });

        // Make sure the new folder is visible even when a search or filter is active.
        _searchText = string.Empty;
        OnPropertyChanged(nameof(SearchText));
        Filter = FolderFilter.All;
        RefreshFolders(lastId);
        StatusMessage = addedCount == 1 ? AppText.FolderRegistered : AppText.FoldersRegistered(addedCount);

        if (problems.Count > 0)
        {
            _interaction.ShowError(string.Join(Environment.NewLine, problems));
        }
    }

    private void OpenSelectedInExplorer()
    {
        if (SelectedFolder is { } folder && Directory.Exists(folder.Path))
        {
            ElevatedToolRunner.OpenExplorer(folder.Path);
        }
    }

    private void RemoveSelectedFolder()
    {
        if (SelectedFolder is null)
        {
            return;
        }

        _config = _configStore.Load();
        if (SelectedFolder.Model.State is FolderLockState.Locked or FolderLockState.Working)
        {
            _interaction.ShowError(AppText.RemoveLockedFolderError);
            return;
        }

        if (!_interaction.Confirm(AppText.RemoveFolderTitle, AppText.RemoveFolderMessage))
        {
            return;
        }

        string removeId = SelectedFolder.Id;
        _configStore.Update(config => config.Folders.RemoveAll(folder => string.Equals(folder.Id, removeId, StringComparison.OrdinalIgnoreCase)) > 0);
        RefreshFolders();
        StatusMessage = AppText.FolderRegistrationRemoved;
    }

    private async Task LockSelectedFolderAsync()
    {
        if (SelectedFolder is null)
        {
            return;
        }

        // Locking is blocked until the master recovery password exists. The button
        // stays enabled on purpose: clicking it explains why the password is needed
        // and offers to run first-time setup right away.
        if (!EnsureMasterConfigured(AppText.MasterNotConfiguredExplanation, "lock"))
        {
            return;
        }

        RegisteredFolder folder = SelectedFolder.Model;
        string modeText = AppText.ModeName(SelectedMode);
        string childText = SelectedMode == LockMode.Quick ? AppText.NoChildItems : AppText.RecursiveChildAcl;
        string recoveryToolPath = TryFindToolPath("FolderGate.RecoveryTool");

        string message =
            $"{AppText.ActualPathLabel}: {folder.Path}{Environment.NewLine}" +
            $"{AppText.ModeLabel}: {modeText}{Environment.NewLine}" +
            $"{AppText.ScopeLabel}: {childText}{Environment.NewLine}" +
            $"{AppText.RecoveryToolName}: {recoveryToolPath}{Environment.NewLine}{Environment.NewLine}" +
            AppText.LockConfirmTail;

        if (!_interaction.Confirm(AppText.LockConfirmTitle, message))
        {
            return;
        }

        await RunHelperAndRefreshAsync("lock", folder, SelectedMode).ConfigureAwait(true);
    }

    private async Task UnlockSelectedFolderAsync()
    {
        if (SelectedFolder is null)
        {
            return;
        }

        _config = _configStore.Load();
        UnlockPasswordRequest? request = _interaction.AskUnlockPassword(AppText.UnlockTitle, AppText.UnlockPasswordMessage, _config.DefaultUnlockMinutes);
        if (request is null)
        {
            return;
        }

        RegisteredFolder folder = _config.Folders.FirstOrDefault(item => item.Id == SelectedFolder.Id) ?? SelectedFolder.Model;
        if (!_passwordService.Verify(request.Password, _config.PasswordFor(folder)))
        {
            _interaction.ShowError(AppText.InvalidPasswordNoAcl);
            return;
        }

        if (request.Duration is not null)
        {
            // Timed unlock registers the automatic re-lock startup entry, which is
            // blocked until the master recovery password is configured. Permanent
            // unlock stays available in every security state.
            if (!EnsureMasterConfigured(AppText.MasterTempUnlockBlockedNoMaster, "temporary-unlock"))
            {
                return;
            }

            await StartTemporaryUnlockAndRefreshAsync(folder, request.Duration.Value).ConfigureAwait(true);
            return;
        }

        await RunHelperAndRefreshAsync("unlock", folder, null).ConfigureAwait(true);

        _config = _configStore.Load();
        RegisteredFolder? refreshed = _config.Folders.FirstOrDefault(item => item.Id == folder.Id);
        if (refreshed is { State: FolderLockState.Unlocked } && Directory.Exists(refreshed.Path))
        {
            ElevatedToolRunner.OpenExplorer(refreshed.Path);
        }
    }

    private async Task StartTemporaryUnlockAndRefreshAsync(RegisteredFolder folder, TimeSpan duration)
    {
        string operationId = Guid.NewGuid().ToString("N");
        Process? process = null;
        try
        {
            BeginFolderOperation(folder.Id, operationId);
            IsBusy = true;
            _activeOperationId = operationId;
            ResetOperationProgress();
            StatusMessage = AppText.TempUnlockRequestingUac;
            _startupRelockService.Install();
            process = _toolRunner.StartHelper("temporary-unlock", folder, operationId, duration: duration);

            DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(10);
            while (DateTimeOffset.UtcNow < deadline)
            {
                ApplyProgressSnapshots();
                _config = _configStore.Load();
                RegisteredFolder? refreshed = _config.Folders.FirstOrDefault(item => item.Id == folder.Id);
                if (refreshed is { State: FolderLockState.TemporarilyUnlocked })
                {
                    RefreshFolders(folder.Id);
                    StatusMessage = AppText.TempUnlockStarted;
                    ElevatedToolRunner.OpenExplorer(refreshed.Path);
                    return;
                }

                if (process.HasExited)
                {
                    int exitCode = process.ExitCode;
                    RefreshFolders(folder.Id);
                    StatusMessage = AppText.LanguageCode == "en"
                        ? $"Temporary unlock process exited. Exit code: {exitCode}"
                        : $"임시 잠금 해제 프로세스가 종료되었습니다. 종료 코드: {exitCode}";
                    if (exitCode != 0)
                    {
                        _interaction.ShowError(AppText.TempUnlockFailed);
                    }

                    return;
                }

                await Task.Delay(500).ConfigureAwait(true);
            }

            RefreshFolders(folder.Id);
            StatusMessage = AppText.TempUnlockCheckTimeout;
            _interaction.ShowError(AppText.TempUnlockCheckFailed);
        }
        catch (Exception ex)
        {
            _config = _configStore.Load();
            RefreshFolders(folder.Id);
            _interaction.ShowError(ex.Message);
            StatusMessage = AppText.TempUnlockGeneralFailure;
        }
        finally
        {
            process?.Dispose();
            _activeOperationId = null;
            IsProgressIndeterminate = false;
            EndFolderOperation();
            IsBusy = false;
        }
    }

    private void ChangePassword()
    {
        _config = _configStore.Load();
        if (_config.Password is not null)
        {
            string? oldPassword = _interaction.AskPassword(AppText.CurrentPasswordTitle, AppText.CurrentPasswordMessage);
            if (oldPassword is null)
            {
                return;
            }

            if (!_passwordService.Verify(oldPassword, _config.Password))
            {
                _interaction.ShowError(AppText.InvalidPasswordNoAcl);
                return;
            }
        }

        string? newPassword = _interaction.AskNewPassword(AppText.ChangePasswordTitle, AppText.ChangePasswordMessage);
        if (newPassword is null)
        {
            return;
        }

        PasswordRecord sharedPassword = _passwordService.CreatePasswordRecord(newPassword);
        _configStore.Update(config =>
        {
            config.Password = sharedPassword;
            return true;
        });
        _config = _configStore.Load();
        StatusMessage = AppText.PasswordChanged;
    }

    private async Task OpenRecoveryToolAsync()
    {
        // The recovery tool enforces master password authentication in its own
        // process; this pre-check only provides a clearer message without a UAC
        // round trip when the tool would refuse to run anyway.
        MasterSecurityState masterState = _masterManager.EvaluateState();
        if (masterState == MasterSecurityState.NotConfigured)
        {
            _interaction.ShowError(AppText.RecoveryMasterNotConfigured);
            return;
        }

        if (masterState == MasterSecurityState.Corrupted)
        {
            _masterManager.LogCorruptedStateDetected("open-recovery-tool");
            _interaction.ShowError(AppText.MasterCorruptedMessage);
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = AppText.RecoveryToolRunning;
            await _toolRunner.OpenRecoveryToolAsync().ConfigureAwait(true);
            _config = _configStore.Load();
            RefreshFolders(SelectedFolder?.Id);
            StatusMessage = AppText.RecoveryToolFinished;
        }
        catch (Exception ex)
        {
            _interaction.ShowError(ex.Message);
            StatusMessage = AppText.RecoveryToolFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool EnsureMasterConfigured(string notConfiguredMessage, string context)
    {
        MasterSecurityState state = _masterManager.EvaluateState();
        if (state == MasterSecurityState.Configured)
        {
            return true;
        }

        if (state == MasterSecurityState.Corrupted)
        {
            _masterManager.LogCorruptedStateDetected(context);
            _interaction.ShowError(AppText.MasterCorruptedMessage);
            return false;
        }

        if (!_interaction.Confirm(AppText.MasterSetupTitle, notConfiguredMessage))
        {
            return false;
        }

        MasterSetupRequest? setup = _interaction.AskMasterPasswordSetup();
        if (setup is null)
        {
            return false;
        }

        try
        {
            _masterManager.SetupInitial(setup.Password, setup.Hint);
            StatusMessage = AppText.MasterSetupDone;
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
        {
            _interaction.ShowError(ex.Message);
            return false;
        }
    }

    private void ChangeMasterPassword()
    {
        MasterSecurityState state = _masterManager.EvaluateState();
        if (state == MasterSecurityState.NotConfigured)
        {
            _interaction.ShowInfo(AppText.MasterFeatureRequiresConfigured);
            return;
        }

        if (state == MasterSecurityState.Corrupted)
        {
            _masterManager.LogCorruptedStateDetected("change-master-password");
            _interaction.ShowError(AppText.MasterCorruptedMessage);
            return;
        }

        MasterChangeRequest? request = _interaction.AskMasterPasswordChange();
        if (request is null)
        {
            return;
        }

        try
        {
            _masterManager.ChangePassword(request.CurrentPassword, request.NewPassword);
            StatusMessage = AppText.MasterPasswordChangedStatus;
            _interaction.ShowInfo(AppText.MasterPasswordChangedStatus);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
        {
            // On any failure the previous credential file is untouched, so the
            // old master password remains valid.
            _interaction.ShowError(ex.Message);
        }
    }

    private void ChangeMasterHint()
    {
        MasterSecurityState state = _masterManager.EvaluateState();
        if (state == MasterSecurityState.NotConfigured)
        {
            _interaction.ShowInfo(AppText.MasterFeatureRequiresConfigured);
            return;
        }

        if (state == MasterSecurityState.Corrupted)
        {
            _masterManager.LogCorruptedStateDetected("change-master-hint");
            _interaction.ShowError(AppText.MasterCorruptedMessage);
            return;
        }

        MasterHintRequest? request = _interaction.AskMasterHintChange(_masterManager.GetHint());
        if (request is null)
        {
            return;
        }

        try
        {
            _masterManager.ChangeHint(request.CurrentPassword, request.Hint);
            StatusMessage = AppText.MasterHintChangedStatus;
            _interaction.ShowInfo(AppText.MasterHintChangedStatus);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
        {
            _interaction.ShowError(ex.Message);
        }
    }

    private void RegisterExplorerMenu()
    {
        try
        {
            _explorerContextMenu.Install();
            StatusMessage = AppText.ExplorerMenuRegistered;
            _interaction.ShowInfo(AppText.ExplorerMenuRegisteredInfo);
        }
        catch (Exception ex)
        {
            _interaction.ShowError(ex.Message);
            StatusMessage = AppText.ExplorerMenuRegisterFailed;
        }
    }

    private void UnregisterExplorerMenu()
    {
        try
        {
            _explorerContextMenu.Uninstall();
            StatusMessage = AppText.ExplorerMenuRemoved;
            _interaction.ShowInfo(AppText.ExplorerMenuRemovedInfo);
        }
        catch (Exception ex)
        {
            _interaction.ShowError(ex.Message);
            StatusMessage = AppText.ExplorerMenuRemoveFailed;
        }
    }

    private async Task RunHelperAndRefreshAsync(string command, RegisteredFolder folder, LockMode? mode)
    {
        string operationId = Guid.NewGuid().ToString("N");
        try
        {
            BeginFolderOperation(folder.Id, operationId);
            IsBusy = true;
            _activeOperationId = operationId;
            ResetOperationProgress();
            StatusMessage = AppText.UacRunStatus;
            Task<int> helperTask = _toolRunner.RunHelperAsync(command, folder, operationId, mode);
            while (!helperTask.IsCompleted)
            {
                ApplyProgressSnapshots();
                await Task.Delay(350).ConfigureAwait(true);
            }

            int exitCode = await helperTask.ConfigureAwait(true);
            ApplyProgressSnapshots();
            _config = _configStore.Load();
            RefreshFolders(folder.Id);

            if (exitCode == 0)
            {
                StatusMessage = AppText.OperationCompleted;
            }
            else
            {
                StatusMessage = AppText.LanguageCode == "en"
                    ? $"Operation failed. Exit code: {exitCode}"
                    : $"작업이 실패했습니다. 종료 코드: {exitCode}";
                _interaction.ShowError(AppText.OperationFailedCheckLogs);
            }
        }
        catch (Exception ex)
        {
            _config = _configStore.Load();
            RefreshFolders(folder.Id);
            _interaction.ShowError(ex.Message);
            StatusMessage = AppText.OperationFailed;
        }
        finally
        {
            _activeOperationId = null;
            IsProgressIndeterminate = false;
            EndFolderOperation();
            IsBusy = false;
        }
    }

    private void CancelCurrentOperation()
    {
        if (_runningOperations.Count == 0)
        {
            return;
        }

        // "Lock all" runs several operations; cancel every one that has not finished.
        foreach (string operationId in _runningOperations.Values)
        {
            _progressStore.RequestCancel(operationId);
        }

        StatusMessage = AppText.CancelRequested;
        RaiseCommandStates();
    }

    private void ResetOperationProgress()
    {
        OperationTotal = 0;
        OperationCompleted = 0;
        OperationFailed = 0;
        OperationCurrentPath = "-";
        OperationElapsed = "00:00:00";
        OperationEta = "-";
        OperationPhase = AppText.Starting;
        OperationProgressPercent = 0;
        IsProgressIndeterminate = true;
        RaiseCommandStates();
    }

    /// <summary>
    /// Reads the progress files of every running operation: each list item shows
    /// its own bar, and the details pane shows the selected folder's operation
    /// (or the first unfinished one while "lock all" runs).
    /// </summary>
    private void ApplyProgressSnapshots()
    {
        OperationProgressSnapshot? focus = null;
        foreach ((string folderId, string operationId) in _runningOperations)
        {
            OperationProgressSnapshot? snapshot = _progressStore.TryLoadProgress(operationId);
            if (snapshot is null)
            {
                continue;
            }

            if (Folders.FirstOrDefault(item => string.Equals(item.Id, folderId, StringComparison.OrdinalIgnoreCase)) is { } item)
            {
                item.IsOperationIndeterminate = snapshot.TotalCount <= 0 && !snapshot.IsCompleted;
                item.OperationProgressPercent = snapshot.IsCompleted
                    ? 100
                    : snapshot.TotalCount <= 0 ? 0 : Math.Clamp(snapshot.CompletedCount * 100.0 / snapshot.TotalCount, 0, 100);
            }

            bool isSelected = string.Equals(folderId, SelectedFolder?.Id, StringComparison.OrdinalIgnoreCase);
            if (isSelected || (focus is null && !snapshot.IsCompleted))
            {
                focus = snapshot;
            }
        }

        if (focus is null)
        {
            UpdateElapsed(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, 0);
            return;
        }

        OperationTotal = focus.TotalCount;
        OperationCompleted = focus.CompletedCount;
        OperationFailed = focus.FailedCount;
        OperationCurrentPath = string.IsNullOrWhiteSpace(focus.CurrentPath) ? "-" : focus.CurrentPath;
        OperationPhase = AppText.FormatPhase(focus.Phase);
        IsProgressIndeterminate = focus.TotalCount <= 0;
        OperationProgressPercent = focus.TotalCount <= 0
            ? 0
            : Math.Clamp(focus.CompletedCount * 100.0 / focus.TotalCount, 0, 100);

        UpdateElapsed(focus.StartedUtc, DateTimeOffset.UtcNow, focus.CompletedCount, focus.TotalCount);
        StatusMessage = focus.Message;
    }

    private void UpdateElapsed(DateTimeOffset startedUtc, DateTimeOffset nowUtc, int completed, int total)
    {
        TimeSpan elapsed = nowUtc - startedUtc;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        OperationElapsed = FormatDuration(elapsed);
        if (completed > 0 && total > completed)
        {
            double secondsPerItem = elapsed.TotalSeconds / completed;
            OperationEta = FormatDuration(TimeSpan.FromSeconds(secondsPerItem * (total - completed)));
        }
        else if (total > 0 && completed >= total)
        {
            OperationEta = "00:00:00";
        }
        else
        {
            OperationEta = "-";
        }
    }

    private static string FormatDuration(TimeSpan value)
    {
        return value.TotalHours >= 1
            ? value.ToString(@"hh\:mm\:ss")
            : value.ToString(@"mm\:ss");
    }

    /// <summary>
    /// Reloads the folder list from storage. Used by the tray icon after a
    /// tray-initiated unlock so an already-open window shows the new state.
    /// </summary>
    public void RefreshFromStorage()
    {
        RefreshFolders(SelectedFolder?.Id);
    }

    /// <summary>
    /// Surfaces an available update in the status bar. Deliberately unobtrusive:
    /// no dialog, no interruption of any running operation.
    /// </summary>
    public void NotifyUpdateAvailable(string latestVersion)
    {
        if (!IsBusy)
        {
            StatusMessage = AppText.UpdateAvailableStatusBar(latestVersion);
        }
    }

    private void RefreshFolders(string? selectedId = null)
    {
        _config = _configStore.Load();
        _lastSeenConfigUpdateUtc = _config.UpdatedUtc;
        Folders.Clear();
        foreach (RegisteredFolder folder in _config.Folders.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase))
        {
            Folders.Add(new FolderItemViewModel(folder)
            {
                IsOperationRunning = IsBusy && _runningOperations.ContainsKey(folder.Id),
                IsOperationIndeterminate = IsProgressIndeterminate,
                OperationProgressPercent = OperationProgressPercent,
                Presence = FolderPresenceProbe.Probe(folder.Path)
            });
        }

        IEnumerable<FolderItemViewModel> visible = FoldersView.Cast<FolderItemViewModel>();
        SelectedFolder = selectedId is null
            ? visible.FirstOrDefault()
            : visible.FirstOrDefault(item => string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase)) ?? visible.FirstOrDefault();

        NotifyListChanged();
        NotifySelectionChanged();
    }

    private void RefreshView()
    {
        string? selectedId = SelectedFolder?.Id;
        FoldersView.Refresh();
        IEnumerable<FolderItemViewModel> visible = FoldersView.Cast<FolderItemViewModel>();
        SelectedFolder = visible.FirstOrDefault(item => string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                         ?? visible.FirstOrDefault();
        NotifyListChanged();
    }

    private void NotifyListChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(LockedCount));
        OnPropertyChanged(nameof(UnlockedCount));
        OnPropertyChanged(nameof(AttentionCount));
        OnPropertyChanged(nameof(HasAttention));
        OnPropertyChanged(nameof(HasFolders));
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(FolderCountText));
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectedTitle));
        OnPropertyChanged(nameof(SelectedPath));
        OnPropertyChanged(nameof(SelectedState));
        OnPropertyChanged(nameof(SelectedStateValue));
        OnPropertyChanged(nameof(SelectedStateGlyph));
        OnPropertyChanged(nameof(SelectedStatusTitle));
        OnPropertyChanged(nameof(SelectedStatusBody));
        OnPropertyChanged(nameof(SelectedLastOperation));
        OnPropertyChanged(nameof(SelectedLastOperationShort));
        OnPropertyChanged(nameof(SelectedLastResult));
        OnPropertyChanged(nameof(SelectedModeText));
        OnPropertyChanged(nameof(SelectedHasWarning));
        OnPropertyChanged(nameof(SelectedWarningText));
        OnPropertyChanged(nameof(IsLockPrimary));
        OnPropertyChanged(nameof(IsRelockPrimary));
        OnPropertyChanged(nameof(IsUnlockPrimary));
        OnPropertyChanged(nameof(IsRecoveryPrimary));
        OnPropertyChanged(nameof(ShowUnlockSecondary));
        OnPropertyChanged(nameof(ShowModeChoice));
        OnPropertyChanged(nameof(CanOpenInExplorer));
        OnPropertyChanged(nameof(IsSelectedBusy));
        OnPropertyChanged(nameof(IsSelectedTemporary));
        OnPropertyChanged(nameof(SelectedRemainingText));
        OnPropertyChanged(nameof(SelectedIsMissing));
        OnPropertyChanged(nameof(SelectedPresenceTitle));
        OnPropertyChanged(nameof(SelectedPresenceBody));
        OnPropertyChanged(nameof(SelectedHasOwnPassword));
        OnPropertyChanged(nameof(SelectedPasswordKind));
        OnPropertyChanged(nameof(SelectedFolderPasswordMenuText));
        OpenInExplorerCommand.RaiseCanExecuteChanged();
        RaiseCommandStates();
        LoadSelectedHistory();
    }

    private void LoadSelectedHistory()
    {
        SelectedHistory.Clear();
        if (SelectedFolder is { } folder)
        {
            foreach (OperationLogRecord record in OperationLogReader.ReadForTarget(_paths.LogFilePath, folder.Id, 12))
            {
                SelectedHistory.Add(new FolderHistoryEntry(
                    FolderItemViewModel.FormatLocalTime(record.TimestampUtc),
                    AppText.HistoryOperationName(record.Operation),
                    record.Message ?? string.Empty,
                    string.Equals(record.Status, "Failed", StringComparison.OrdinalIgnoreCase)));
            }
        }

        OnPropertyChanged(nameof(HasSelectedHistory));
    }

    private void BeginFolderOperation(string folderId, string operationId)
    {
        _runningOperations[folderId] = operationId;
        if (Folders.FirstOrDefault(item => string.Equals(item.Id, folderId, StringComparison.OrdinalIgnoreCase)) is { } item)
        {
            item.IsOperationIndeterminate = true;
            item.OperationProgressPercent = 0;
            item.IsOperationRunning = true;
        }

        OnPropertyChanged(nameof(IsSelectedBusy));
    }

    private void EndFolderOperation()
    {
        foreach (FolderItemViewModel item in Folders.Where(item => _runningOperations.ContainsKey(item.Id)))
        {
            item.IsOperationRunning = false;
        }

        _runningOperations.Clear();
        OnPropertyChanged(nameof(IsSelectedBusy));
    }

    // ===== Periodic sync (called by the main window every two seconds) =====

    /// <summary>
    /// Picks up changes other processes saved (the elevated helper re-locking a
    /// temporary unlock, the Explorer unlock prompt, the tray), refreshes the
    /// remaining time of temporary unlocks and, every few calls, checks whether
    /// registered folders still exist. Items are updated in place so selection and
    /// scrolling stay put.
    /// </summary>
    public void SyncFromStorage()
    {
        _syncTicks++;
        bool probePresence = _syncTicks % 5 == 0;
        FolderGateConfig config;
        try
        {
            config = _configStore.Load();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            return;
        }

        bool changed = config.UpdatedUtc != _lastSeenConfigUpdateUtc;
        if (changed)
        {
            List<RegisteredFolder> saved = config.Folders.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
            bool sameFolders = saved.Count == Folders.Count &&
                               saved.Select(item => item.Id).SequenceEqual(Folders.Select(item => item.Id), StringComparer.OrdinalIgnoreCase);
            if (!sameFolders)
            {
                RefreshFolders(SelectedFolder?.Id);
                return;
            }

            _config = config;
            _lastSeenConfigUpdateUtc = config.UpdatedUtc;
            for (int index = 0; index < saved.Count; index++)
            {
                Folders[index].Update(saved[index]);
            }
        }

        foreach (FolderItemViewModel item in Folders)
        {
            item.RefreshClock();
            if (probePresence)
            {
                item.Presence = FolderPresenceProbe.Probe(item.Path);
            }
        }

        if (changed || probePresence)
        {
            NotifyListChanged();
            NotifySelectionChanged();
        }
        else
        {
            OnPropertyChanged(nameof(SelectedRemainingText));
            OnPropertyChanged(nameof(SelectedStatusBody));
        }
    }

    // ===== Lock all =====

    private IEnumerable<RegisteredFolder> LockableFolders() => Folders
        .Where(item => !item.IsMissing && BatchLockRequest.IsLockable(item.State))
        .Select(item => item.Model);

    private async Task LockAllAsync()
    {
        _config = _configStore.Load();
        List<RegisteredFolder> targets = _config.Folders
            .Where(folder => BatchLockRequest.IsLockable(folder.State) &&
                             FolderPresenceProbe.Probe(folder.Path) == FolderPresence.Present)
            .OrderBy(folder => folder.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        if (targets.Count == 0)
        {
            _interaction.ShowInfo(AppText.LockAllNothingToLock);
            return;
        }

        if (!EnsureMasterConfigured(AppText.MasterNotConfiguredExplanation, "lock-all") ||
            !_interaction.Confirm(AppText.LockAll, AppText.LockAllConfirmMessage(targets.Count)))
        {
            return;
        }

        List<string> operationIds = targets.Select(_ => Guid.NewGuid().ToString("N")).ToList();
        try
        {
            for (int index = 0; index < targets.Count; index++)
            {
                BeginFolderOperation(targets[index].Id, operationIds[index]);
            }

            IsBusy = true;
            ResetOperationProgress();
            StatusMessage = AppText.UacRunStatus;
            Task<int> helperTask = _toolRunner.RunLockManyAsync(targets, operationIds);
            while (!helperTask.IsCompleted)
            {
                ApplyProgressSnapshots();
                await Task.Delay(350).ConfigureAwait(true);
            }

            int exitCode = await helperTask.ConfigureAwait(true);
            ApplyProgressSnapshots();
            RefreshFolders(SelectedFolder?.Id);
            if (exitCode == 0)
            {
                StatusMessage = AppText.LockAllCompleted;
            }
            else
            {
                StatusMessage = AppText.LockAllPartlyFailed;
                _interaction.ShowError(AppText.LockAllPartlyFailed);
            }
        }
        catch (Exception ex)
        {
            RefreshFolders(SelectedFolder?.Id);
            _interaction.ShowError(ex.Message);
            StatusMessage = AppText.OperationFailed;
        }
        finally
        {
            foreach (string operationId in operationIds)
            {
                _progressStore.ClearCancel(operationId);
            }

            IsProgressIndeterminate = false;
            EndFolderOperation();
            IsBusy = false;
        }
    }

    // ===== Temporary unlock: lock now, extend =====

    /// <summary>
    /// Moves the re-lock time of the selected temporary unlock to now. The
    /// elevated process that holds the temporary unlock (or the login re-lock
    /// runner) re-locks within about a second without another UAC prompt. If
    /// nothing picks it up (for example that process was ended), a normal lock
    /// with a UAC prompt is started instead.
    /// </summary>
    private async Task RelockSelectedNowAsync()
    {
        if (SelectedFolder is not { } selected)
        {
            return;
        }

        string folderId = selected.Id;
        try
        {
            if (!UpdateSavedFolder(folderId, folder =>
                {
                    if (folder.State != FolderLockState.TemporarilyUnlocked)
                    {
                        return false;
                    }

                    folder.TemporaryUnlockUntilUtc = DateTimeOffset.UtcNow;
                    return true;
                }))
            {
                return;
            }

            new JsonOperationLogger(_paths).Info(Guid.NewGuid().ToString("N"), folderId, "RelockRequested", selected.Path, AppText.RelockRequested);
            StatusMessage = AppText.RelockRequested;

            // Block other operations while the waiting process takes the request,
            // so a manual lock cannot race the automatic re-lock.
            IsBusy = true;
            try
            {
                DateTimeOffset giveUpAt = DateTimeOffset.UtcNow.AddSeconds(4);
                while (DateTimeOffset.UtcNow < giveUpAt)
                {
                    await Task.Delay(400).ConfigureAwait(true);
                    if (_configStore.FindFolder(folderId)?.State != FolderLockState.TemporarilyUnlocked)
                    {
                        SyncFromStorage();
                        return;
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }

            // Nothing picked the request up (the waiting process is gone): lock the
            // usual way, with the same master-password guard as every other lock.
            RegisteredFolder? stillOpen = _configStore.FindFolder(folderId);
            if (stillOpen?.State == FolderLockState.TemporarilyUnlocked &&
                EnsureMasterConfigured(AppText.MasterNotConfiguredExplanation, "relock-now"))
            {
                await RunHelperAndRefreshAsync("lock", stillOpen, stillOpen.Mode).ConfigureAwait(true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _interaction.ShowError(ex.Message);
        }
    }

    private void ExtendSelectedUnlock(object? parameter)
    {
        if (SelectedFolder is not { } selected ||
            !int.TryParse(parameter?.ToString(), out int minutes) || minutes <= 0)
        {
            return;
        }

        try
        {
            DateTimeOffset? newDeadline = null;
            bool updated = UpdateSavedFolder(selected.Id, folder =>
            {
                if (folder.State != FolderLockState.TemporarilyUnlocked || folder.TemporaryUnlockUntilUtc is not { } until)
                {
                    return false;
                }

                // Never further away than the longest unlock option (one day from now).
                DateTimeOffset now = DateTimeOffset.UtcNow;
                DateTimeOffset start = until > now ? until : now;
                DateTimeOffset latest = now + TemporaryUnlockSchedule.MaximumRemaining;
                DateTimeOffset extended = start.AddMinutes(minutes);
                folder.TemporaryUnlockUntilUtc = extended < latest ? extended : latest;
                folder.LastOperationUtc = now;
                folder.LastResult = AppText.ExtendedUntil(FolderItemViewModel.FormatLocalTime(folder.TemporaryUnlockUntilUtc.Value));
                newDeadline = folder.TemporaryUnlockUntilUtc;
                return true;
            });

            if (updated && newDeadline is { } deadline)
            {
                string time = FolderItemViewModel.FormatLocalTime(deadline);
                new JsonOperationLogger(_paths).Info(Guid.NewGuid().ToString("N"), selected.Id, "TemporaryUnlockExtended", selected.Path, AppText.ExtendedUntil(time));
                StatusMessage = AppText.ExtendedUntil(time);
                SyncFromStorage();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _interaction.ShowError(ex.Message);
        }
    }

    /// <summary>
    /// Changes one folder under the configuration lock when <paramref name="change"/>
    /// returns true. Returns whether it was saved.
    /// </summary>
    private bool UpdateSavedFolder(string folderId, Func<RegisteredFolder, bool> change)
    {
        bool saved = _configStore.Update(config =>
        {
            RegisteredFolder? folder = config.Folders.FirstOrDefault(item => string.Equals(item.Id, folderId, StringComparison.OrdinalIgnoreCase));
            return folder is not null && change(folder);
        });
        if (saved)
        {
            _config = _configStore.Load();
        }

        return saved;
    }
    // ===== Per-folder password =====

    private void SetSelectedFolderPassword()
    {
        try
        {
            SetSelectedFolderPasswordCore();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _interaction.ShowError(ex.Message);
        }
    }

    private void ClearSelectedFolderPassword()
    {
        try
        {
            ClearSelectedFolderPasswordCore();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _interaction.ShowError(ex.Message);
        }
    }

    private void SetSelectedFolderPasswordCore()
    {
        if (SelectedFolder is not { } selected || !VerifyCurrentFolderPassword(selected))
        {
            return;
        }

        string? newPassword = _interaction.AskNewPassword(AppText.FolderPasswordTitle, AppText.FolderPasswordNewMessage(selected.DisplayName));
        if (newPassword is null)
        {
            return;
        }

        PasswordRecord record = _passwordService.CreatePasswordRecord(newPassword);
        if (UpdateSavedFolder(selected.Id, folder =>
            {
                folder.Password = record;
                return true;
            }))
        {
            new JsonOperationLogger(_paths).Info(Guid.NewGuid().ToString("N"), selected.Id, "FolderPassword", selected.Path, AppText.FolderPasswordLog);
            StatusMessage = AppText.FolderPasswordSetDone;
            _interaction.ShowInfo(AppText.FolderPasswordSetDone);
            RefreshFolders(selected.Id);
        }
    }

    private void ClearSelectedFolderPasswordCore()
    {
        if (SelectedFolder is not { HasOwnPassword: true } selected || !VerifyCurrentFolderPassword(selected))
        {
            return;
        }

        if (UpdateSavedFolder(selected.Id, folder =>
            {
                folder.Password = null;
                return true;
            }))
        {
            new JsonOperationLogger(_paths).Info(Guid.NewGuid().ToString("N"), selected.Id, "FolderPassword", selected.Path, AppText.FolderPasswordLog);
            StatusMessage = AppText.FolderPasswordClearedDone;
            _interaction.ShowInfo(AppText.FolderPasswordClearedDone);
            RefreshFolders(selected.Id);
        }
    }

    /// <summary>
    /// Asks for the password that unlocks the folder today (its own, or the shared
    /// one) before its password can be changed. True when it matched, or when no
    /// password exists yet.
    /// </summary>
    private bool VerifyCurrentFolderPassword(FolderItemViewModel selected)
    {
        _config = _configStore.Load();
        RegisteredFolder? folder = _config.Folders.FirstOrDefault(item => item.Id == selected.Id);
        PasswordRecord? current = folder is null ? null : _config.PasswordFor(folder);
        if (current is null)
        {
            return folder is not null;
        }

        string? entered = _interaction.AskPassword(AppText.FolderPasswordTitle, AppText.FolderPasswordCurrentMessage(selected.DisplayName));
        if (entered is null)
        {
            return false;
        }

        if (!_passwordService.Verify(entered, current))
        {
            _interaction.ShowError(AppText.InvalidPasswordNoAcl);
            return false;
        }

        return true;
    }

    private string TryFindToolPath(string projectName)
    {
        try
        {
            return new ToolLocator(_paths).FindExecutable(projectName);
        }
        catch
        {
            return AppText.ToolNotFoundHint;
        }
    }

    private void RaiseCommandStates()
    {
        RemoveFolderCommand.RaiseCanExecuteChanged();
        LockCommand.RaiseCanExecuteChanged();
        UnlockCommand.RaiseCanExecuteChanged();
        ChangePasswordCommand.RaiseCanExecuteChanged();
        OpenRecoveryToolCommand.RaiseCanExecuteChanged();
        RegisterExplorerMenuCommand.RaiseCanExecuteChanged();
        UnregisterExplorerMenuCommand.RaiseCanExecuteChanged();
        CancelOperationCommand.RaiseCanExecuteChanged();
        ChangeMasterPasswordCommand.RaiseCanExecuteChanged();
        ChangeMasterHintCommand.RaiseCanExecuteChanged();
        LockAllCommand.RaiseCanExecuteChanged();
        RelockNowCommand.RaiseCanExecuteChanged();
        ExtendUnlockCommand.RaiseCanExecuteChanged();
        SetFolderPasswordCommand.RaiseCanExecuteChanged();
        ClearFolderPasswordCommand.RaiseCanExecuteChanged();
    }
}
