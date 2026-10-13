using System.Windows;
using FolderGate.App.Controls;
using FolderGate.App.Services;
using FolderGate.Core.Localization;
using FolderGate.Core.Storage;
using Microsoft.Win32;

namespace FolderGate.App;

public enum MigrationPromptChoice
{
    Skip,
    MigrateSelected
}

/// <summary>
/// Startup prompt shown in the installed layout when legacy portable data is
/// found and no data exists at the new location. The window only collects the
/// user's choice; the caller performs analysis, confirmation, and migration.
/// </summary>
public partial class MigrationPromptWindow : ThemedWindow
{
    public MigrationPromptWindow(IReadOnlyList<MigrationCandidate> candidates)
    {
        InitializeComponent();
        CandidateList.ItemsSource = candidates;
        if (candidates.Count > 0)
        {
            CandidateList.SelectedIndex = 0;
        }
    }

    public MigrationPromptChoice Choice { get; private set; } = MigrationPromptChoice.Skip;

    public string? SelectedRootPath { get; private set; }

    private void Migrate_Click(object sender, RoutedEventArgs e)
    {
        if (CandidateList.SelectedItem is not MigrationCandidate candidate)
        {
            AppDialog.Warning(this, AppText.MigrationSourceConfigMissing, AppText.MigrationPromptTitle);
            return;
        }

        Choice = MigrationPromptChoice.MigrateSelected;
        SelectedRootPath = candidate.RootPath;
        DialogResult = true;
    }

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog dialog = new()
        {
            Title = AppText.MigrationSelectFolderTitle,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (!PortableMigrationService.HasLegacyData(dialog.FolderName))
        {
            AppDialog.Warning(this, AppText.MigrationNoLegacyInSelected, AppText.MigrationPromptTitle);
            return;
        }

        Choice = MigrationPromptChoice.MigrateSelected;
        SelectedRootPath = dialog.FolderName;
        DialogResult = true;
    }
}
