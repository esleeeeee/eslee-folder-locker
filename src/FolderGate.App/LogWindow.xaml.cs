using System.Windows;
using FolderGate.App.Controls;

namespace FolderGate.App;

public partial class LogWindow : ThemedWindow
{
    public LogWindow(string logText)
    {
        InitializeComponent();
        LogText.Text = logText;
    }
}
