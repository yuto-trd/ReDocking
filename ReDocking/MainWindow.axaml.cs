using System.Collections;

using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Platform;

using Reactive.Bindings;

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

    private void OnEdgeBarButtonDrop(object? sender, ButtonDropEventArgs e)
    {
        ReactiveCollection<ToolWindowViewModel>? GetItemsSource(EdgeBar edgeBar, EdgeBarButtonLocation location)
        {
            return location switch
            {
                EdgeBarButtonLocation.Top => edgeBar.TopToolsSource,
                EdgeBarButtonLocation.Middle => edgeBar.ToolsSource,
                EdgeBarButtonLocation.Bottom => edgeBar.BottomToolsSource,
                _ => null
            } as ReactiveCollection<ToolWindowViewModel>;
        }

        var oldItems = GetItemsSource(e.SourceEdgeBar, e.SourceLocation);
        var newItems = GetItemsSource(e.DestinationEdgeBar, e.DestinationLocation);

        if (oldItems == null || newItems == null || e.Item is not ToolWindowViewModel item)
        {
            return;
        }

        if (oldItems == newItems)
        {
            var sourceIndex = oldItems.IndexOf(item);
            var destinationIndex = e.DestinationIndex;
            if (sourceIndex < destinationIndex)
            {
                destinationIndex--;
            }
            oldItems.Move(sourceIndex, destinationIndex);
        }
        else
        {
            oldItems.Remove(item);
            newItems.Insert(e.DestinationIndex, new ToolWindowViewModel(item.Name.Value, item.Icon.Value));   
        }

        e.Handled = true;
    }
}