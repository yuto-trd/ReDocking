using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Platform;

namespace ReDocking;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        ExtendClientAreaChromeHints =
            ExtendClientAreaChromeHints.SystemChrome | ExtendClientAreaChromeHints.OSXThickTitleBar;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = 40;
        DataContext = new MainWindowViewModel();
        InitializeComponent();
    }
}