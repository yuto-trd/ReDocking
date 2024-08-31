using System;
using System.Collections.Specialized;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Platform;

using Reactive.Bindings;

using ReDocking.ViewModels;

namespace ReDocking.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        ExtendClientAreaChromeHints =
            ExtendClientAreaChromeHints.SystemChrome | ExtendClientAreaChromeHints.OSXThickTitleBar;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = 40;
        _viewModel = new MainWindowViewModel();
        _viewModel.FloatingWindows.CollectionChanged += FloatingWindowsOnCollectionChanged;
        DataContext = _viewModel;
        InitializeComponent();
    }

    private void FloatingWindowsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (ToolWindowViewModel item in e.NewItems!)
            {
                _ = new ToolWindow(item, this);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (ToolWindowViewModel item in e.OldItems!)
            {
                this.OwnedWindows.FirstOrDefault(x => x.DataContext == item)?.Close();
            }
        }
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
            var newItem = new ToolWindowViewModel(item.Name.Value, item.Icon.Value, item.Content.Value);
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

    private void OnSideBarButtonDisplayModeChanged(object? sender, SideBarButtonDisplayModeChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;
        if (e.Item is not ToolWindowViewModel item || item.DisplayMode.Value == e.DisplayMode) return;
        item.IsSelected.Value = false;
        item.DisplayMode.Value = e.DisplayMode;
        item.IsSelected.Value = true;
        if (e.DisplayMode == DockableDisplayMode.Floating)
        {
            viewModel.FloatingWindows.Add(item);
        }
        else
        {
            viewModel.FloatingWindows.Remove(item);
        }

        e.Handled = true;
    }
}