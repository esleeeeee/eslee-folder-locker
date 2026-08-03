using System.Windows;
using System.Windows.Controls;

namespace FolderGate.App.Controls;

/// <summary>
/// Password input with a show/hide toggle. The entered value is passed through
/// exactly as typed: no trimming, no case changes, no Unicode normalization.
/// </summary>
public partial class RevealablePasswordBox : System.Windows.Controls.UserControl
{
    private bool _syncing;

    public RevealablePasswordBox()
    {
        InitializeComponent();
    }

    public string Password
    {
        get => RevealToggle.IsChecked == true ? VisibleInput.Text : HiddenInput.Password;
        set
        {
            _syncing = true;
            HiddenInput.Password = value;
            VisibleInput.Text = value;
            _syncing = false;
        }
    }

    public void FocusInput()
    {
        if (RevealToggle.IsChecked == true)
        {
            VisibleInput.Focus();
        }
        else
        {
            HiddenInput.Focus();
        }
    }

    private void HiddenInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        VisibleInput.Text = HiddenInput.Password;
        _syncing = false;
    }

    private void VisibleInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        HiddenInput.Password = VisibleInput.Text;
        _syncing = false;
    }

    private void RevealToggle_Changed(object sender, RoutedEventArgs e)
    {
        bool reveal = RevealToggle.IsChecked == true;
        VisibleInput.Visibility = reveal ? Visibility.Visible : Visibility.Collapsed;
        HiddenInput.Visibility = reveal ? Visibility.Collapsed : Visibility.Visible;
        FocusInput();
    }
}
