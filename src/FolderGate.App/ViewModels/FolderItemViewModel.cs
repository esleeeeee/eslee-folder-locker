using System.Globalization;
using FolderGate.App.Controls;
using FolderGate.Core.Formatting;
using FolderGate.Core.Localization;
using FolderGate.Core.Models;
using FolderGate.Core.Validation;

namespace FolderGate.App.ViewModels;

public sealed class FolderItemViewModel : ObservableObject
{
    private bool _isOperationRunning;
    private double _operationProgressPercent;
    private bool _isOperationIndeterminate;
    private FolderPresence _presence = FolderPresence.Present;

    public FolderItemViewModel(RegisteredFolder model)
    {
        Model = model;
    }

    public RegisteredFolder Model { get; private set; }

    public string Id => Model.Id;

    public string DisplayName => Model.DisplayName;

    public string Path => Model.Path;

    public FolderLockState State => Model.State;

    public string ModeText => AppText.ModeName(Model.Mode);

    public string StateText => AppText.StateName(Model.State);

    /// <summary>State shown in the list: missing folders and offline drives win over the lock state.</summary>
    public string ListStateText => Presence switch
    {
        FolderPresence.Missing => AppText.StateMissing,
        FolderPresence.DriveUnavailable => AppText.StateDriveOffline,
        _ when Model.State == FolderLockState.TemporarilyUnlocked && RemainingText is { } remaining => remaining,
        _ => StateText
    };

    public string StateGlyph => Presence != FolderPresence.Present
        ? Glyphs.Warning
        : Model.State switch
        {
            FolderLockState.Locked => Glyphs.Lock,
            FolderLockState.TemporarilyUnlocked => Glyphs.Clock,
            FolderLockState.Working => Glyphs.Sync,
            FolderLockState.RecoveryRequired => Glyphs.Warning,
            _ => Glyphs.Unlock
        };

    public FolderPresence Presence
    {
        get => _presence;
        set
        {
            if (SetProperty(ref _presence, value))
            {
                OnPropertyChanged(nameof(IsMissing));
                OnPropertyChanged(nameof(ListStateText));
                OnPropertyChanged(nameof(StateGlyph));
            }
        }
    }

    public bool IsMissing => Presence != FolderPresence.Present;

    /// <summary>The name screen readers announce for the list item.</summary>
    public override string ToString() => DisplayName;

    public bool HasOwnPassword => Model.Password is not null;

    public string LastOperationText => Model.LastOperationUtc is null
        ? "-"
        : LocalTimeFormatter.FormatLocal(Model.LastOperationUtc.Value);

    /// <summary>Last operation time in local time without seconds or zone label.</summary>
    public string LastOperationShortText => Model.LastOperationUtc is null
        ? "-"
        : Model.LastOperationUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Local re-lock time of a temporary unlock, or null when unknown.</summary>
    public string? RelockTimeText => Model.TemporaryUnlockUntilUtc is { } until ? FormatLocalTime(until) : null;

    /// <summary>"38 min left" for a temporary unlock; null otherwise.</summary>
    public string? RemainingText => Model.State == FolderLockState.TemporarilyUnlocked && Model.TemporaryUnlockUntilUtc is { } until
        ? AppText.RemainingTime(until - DateTimeOffset.UtcNow)
        : null;

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

    /// <summary>Replaces the saved data (after a periodic reload) and refreshes every binding.</summary>
    public void Update(RegisteredFolder model)
    {
        Model = model;
        OnPropertyChanged(string.Empty);
    }

    /// <summary>Refreshes time-dependent text such as the remaining temporary unlock time.</summary>
    public void RefreshClock()
    {
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(ListStateText));
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
        FolderFilter.Attention => Model.State is FolderLockState.Working or FolderLockState.RecoveryRequired || IsMissing,
        _ => true
    };

    public static string FormatLocalTime(DateTimeOffset utc)
    {
        DateTimeOffset local = utc.ToLocalTime();
        string format = local.Date == DateTimeOffset.Now.Date ? "HH:mm" : "yyyy-MM-dd HH:mm";
        return local.ToString(format, CultureInfo.InvariantCulture);
    }
}

public enum FolderFilter
{
    All,
    Locked,
    Unlocked,
    Attention
}

/// <summary>One line of the per-folder history in the details pane.</summary>
public sealed record FolderHistoryEntry(string Time, string Operation, string Message, bool IsFailure);
