using System.Windows;
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
public partial class MigrationPromptWindow : Window
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
            System.Windows.MessageBox.Show(this, AppText.MigrationSourceConfigMissing, AppText.MigrationPromptTitle,
                MessageBoxButton.OK, MessageBoxImage.Warning);
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
            System.Windows.MessageBox.Show(this, AppText.MigrationNoLegacyInSelected, AppText.MigrationPromptTitle,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Choice = MigrationPromptChoice.MigrateSelected;
        SelectedRootPath = dialog.FolderName;
        DialogResult = true;
    }
}
