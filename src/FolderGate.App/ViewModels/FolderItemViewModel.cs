using System.Globalization;
using FolderGate.App.Controls;
using FolderGate.Core.Formatting;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;

namespace FolderGate.App.ViewModels;

public sealed class FolderItemViewModel : ObservableObject
{
    private bool _isOperationRunning;
    private double _operationProgressPercent;
    private bool _isOperationIndeterminate;

    public FolderItemViewModel(RegisteredFolder model)
    {
        Model = model;
    }

    public RegisteredFolder Model { get; }

    public string Id => Model.Id;

    public string DisplayName => Model.DisplayName;

    public string Path => Model.Path;

    public FolderLockState State => Model.State;

    public string ModeText => AppText.ModeName(Model.Mode);

    public string StateText => AppText.StateName(Model.State);

    public string StateGlyph => Model.State switch
    {
        FolderLockState.Locked => Glyphs.Lock,
        FolderLockState.TemporarilyUnlocked => Glyphs.Clock,
        FolderLockState.Working => Glyphs.Sync,
        FolderLockState.RecoveryRequired => Glyphs.Warning,
        _ => Glyphs.Unlock
    };

    public string LastOperationText => Model.LastOperationUtc is null
        ? "-"
        : LocalTimeFormatter.FormatLocal(Model.LastOperationUtc.Value);

    /// <summary>Last operation time in local time without seconds or zone label.</summary>
    public string LastOperationShortText => Model.LastOperationUtc is null
        ? "-"
        : Model.LastOperationUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Local re-lock time of a temporary unlock, or null when unknown.</summary>
    public string? RelockTimeText
    {
        get
        {
            if (Model.TemporaryUnlockUntilUtc is not { } until)
            {
                return null;
            }

            DateTimeOffset local = until.ToLocalTime();
            string format = local.Date == DateTimeOffset.Now.Date ? "HH:mm" : "yyyy-MM-dd HH:mm";
            return local.ToString(format, CultureInfo.InvariantCulture);
        }
    }

    public string LastResult => string.IsNullOrWhiteSpace(Model.LastResult) ? "-" : Model.LastResult;

    public string WarningText => Model.HasReparsePointWarning ? AppText.ReparsePointShort : string.Empty;

    public bool HasWarning => Model.HasReparsePointWarning;

    /// <summary>True while this app instance runs a lock/unlock operation for the folder.</summary>
    public bool IsOperationRunning
    {
        get => _isOperationRunning;
        set => SetProperty(ref _isOperationRunning, value);
    }

    public double OperationProgressPercent
    {
        get => _operationProgressPercent;
        set => SetProperty(ref _operationProgressPercent, value);
    }

    public bool IsOperationIndeterminate
    {
        get => _isOperationIndeterminate;
        set => SetProperty(ref _isOperationIndeterminate, value);
    }

    public bool MatchesSearch(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        string term = searchText.Trim();
        return DisplayName.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
               Path.Contains(term, StringComparison.CurrentCultureIgnoreCase);
    }

    public bool MatchesFilter(FolderFilter filter) => filter switch
    {
        FolderFilter.Locked => Model.State == FolderLockState.Locked,
        FolderFilter.Unlocked => Model.State is FolderLockState.Unlocked or FolderLockState.TemporarilyUnlocked,
        FolderFilter.Attention => Model.State is FolderLockState.Working or FolderLockState.RecoveryRequired,
        _ => true
    };
}

public enum FolderFilter
{
    All,
    Locked,
    Unlocked,
    Attention
}
