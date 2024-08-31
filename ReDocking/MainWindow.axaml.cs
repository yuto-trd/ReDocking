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

    private void OnSideBarButtonDrop(object? sender, SideBarButtonMoveEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;
        var oldItems = GetItemsSource(viewModel, e.SourceLocation);
        var oldSelectedItem = GetSelectedItem(viewModel, e.SourceLocation);
        var newItems = GetItemsSource(viewModel, e.DestinationLocation);

        if (e.Item is not ToolWindowViewModel item)
        {
            return;
        }

        if (oldSelectedItem.Value == item)
        {
            oldSelectedItem.Value = null;
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
            item.IsSelected.Value = true;
        }
        else
        {
            oldItems.Remove(item);
            var newItem = new ToolWindowViewModel(item.Name.Value, item.Icon.Value);
            newItems.Insert(e.DestinationIndex, newItem);
            newItem.IsSelected.Value = true;
        }

        e.Handled = true;
    }


    private static ReactiveCollection<ToolWindowViewModel> GetItemsSource(MainWindowViewModel viewModel,
        DockAreaLocation location)
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

    private static ReactiveProperty<ToolWindowViewModel?> GetSelectedItem(MainWindowViewModel viewModel,
        DockAreaLocation location)
    {
        return location switch
        {
            DockAreaLocation.Left => viewModel.SelectedLeftTool,
            DockAreaLocation.Right => viewModel.SelectedRightTool,
            DockAreaLocation.TopLeft => viewModel.SelectedLeftTopTool,
            DockAreaLocation.BottomLeft => viewModel.SelectedLeftBottomTool,
            DockAreaLocation.TopRight => viewModel.SelectedRightTopTool,
            DockAreaLocation.BottomRight => viewModel.SelectedRightBottomTool,
            _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
        };
    }
}