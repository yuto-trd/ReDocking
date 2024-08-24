using System;
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

    private void OnEdgeBarButtonDrop(object? sender, EdgeBarButtonMoveEventArgs e)
    {
        ReactiveCollection<ToolWindowViewModel>? GetItemsSource(MainWindowViewModel viewModel, DockAreaLocation location)
        {
            return location switch
            {
                DockAreaLocation.Left => viewModel.LeftTools,
                DockAreaLocation.Right => viewModel.RightTools,
                DockAreaLocation.TopLeft => viewModel.LeftTopTools,
                DockAreaLocation.BottomLeft => viewModel.LeftBottomTools,
                DockAreaLocation.TopRight => viewModel.RightTopTools,
                DockAreaLocation.BottomRight => viewModel.RightBottomTools,
                _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
            };
        }

        if (DataContext is not MainWindowViewModel viewModel) return;
        var oldItems = GetItemsSource(viewModel, e.SourceLocation);
        var newItems = GetItemsSource(viewModel, e.DestinationLocation);

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