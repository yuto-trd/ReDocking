using System;
using System.Collections;
using System.Linq;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ReDocking;

public class EdgeBar : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable> TopToolsSourceProperty =
        AvaloniaProperty.Register<EdgeBar, IEnumerable>(nameof(TopToolsSource));

    public static readonly StyledProperty<IEnumerable> ToolsSourceProperty =
        AvaloniaProperty.Register<EdgeBar, IEnumerable>(nameof(ToolsSource));

    public static readonly StyledProperty<IEnumerable> BottomToolsSourceProperty =
        AvaloniaProperty.Register<EdgeBar, IEnumerable>(nameof(BottomToolsSource));

    public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
        AvaloniaProperty.Register<EdgeBar, IDataTemplate>(nameof(ItemTemplate));

    public static readonly StyledProperty<DockAreaLocation> LocationProperty =
        AvaloniaProperty.Register<EdgeBar, DockAreaLocation>(nameof(Location));

    private ItemsControl? _topTools;
    private ItemsControl? _tools;
    private ItemsControl? _bottomTools;
    private Grid? _grid;
    private Border? _divider;

    private EdgeBarButton? _dragGhost;
    private AdornerLayer? _layer;

    public EdgeBar()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    public IDataTemplate ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public IEnumerable TopToolsSource
    {
        get => GetValue(TopToolsSourceProperty);
        set => SetValue(TopToolsSourceProperty, value);
    }

    public IEnumerable ToolsSource
    {
        get => GetValue(ToolsSourceProperty);
        set => SetValue(ToolsSourceProperty, value);
    }

    public IEnumerable BottomToolsSource
    {
        get => GetValue(BottomToolsSourceProperty);
        set => SetValue(BottomToolsSourceProperty, value);
    }

    public DockAreaLocation Location
    {
        get => GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    internal void SetGridHitTestVisible(bool value)
    {
        if (_grid == null) return;
        _grid.IsHitTestVisible = value;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _topTools = e.NameScope.Get<ItemsControl>("PART_TopTools");
        _tools = e.NameScope.Get<ItemsControl>("PART_Tools");
        _bottomTools = e.NameScope.Get<ItemsControl>("PART_BottomTools");
        _grid = e.NameScope.Get<Grid>("PART_Grid");
        _divider = e.NameScope.Get<Border>("PART_Divider");
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var position = this.PointToScreen(e.GetPosition(this));
        (DockAreaLocation location, int index) = DetermineLocation(position);
        OnDragLeave(sender, e);

        try
        {
            if (!e.Data.Contains("EdgeBarButton") ||
                e.Data.Get("EdgeBarButton") is not EdgeBarButton { DockLocation: not null } button) return;

            if (index < 0) return;

            var edgeBar = button.FindAncestorOfType<EdgeBar>();
            if (edgeBar == null) return;

            var args = new EdgeBarButtonMoveEventArgs(ReDockHost.ButtonMoveEvent, this)
            {
                Item = button.DataContext,
                Button = button,
                SourceEdgeBar = edgeBar,
                SourceLocation = button.DockLocation.Value,
                DestinationEdgeBar = this,
                DestinationLocation = location | Location,
                DestinationIndex = index
            };
            RaiseEvent(args);

            if (args.Handled) return;

            var newItemsSource = location switch
            {
                DockAreaLocation.Top => TopToolsSource,
                DockAreaLocation.Bottom => BottomToolsSource,
                _ => ToolsSource,
            };

            var itemsControl = button.FindAncestorOfType<ItemsControl>();
            var items = itemsControl?.ItemsSource;
            if (items is not IList oldSource) return;
            if (newItemsSource is not IList newSource) return;

            if (oldSource.Contains(button.DataContext))
            {
                oldSource.Remove(button.DataContext);
                newSource.Insert(index, button.DataContext);
            }
            else if (oldSource.Contains(button))
            {
                oldSource.Remove(button);
                newSource.Insert(index, button);
            }
        }finally
        {
            UpdateDividerVisibility();
        }
    }

    private void UpdateDividerVisibility()
    {
        if (_divider != null && _topTools != null && _tools != null)
        {
            _divider.IsVisible = _topTools.ItemCount > 0 && _tools.ItemCount > 0;
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (_topTools == null || _tools == null || _bottomTools == null) return;
        if (e.Data.Contains("EdgeBarButton"))
        {
            SetGridHitTestVisible(true);
            foreach (Control item in _topTools.GetRealizedContainers()
                         .Concat(_tools.GetRealizedContainers())
                         .Concat(_bottomTools.GetRealizedContainers()))
            {
                item.Margin = default;
                if (item is ContentPresenter { Child: { } child })
                {
                    ToolTip.SetServiceEnabled(child, true);
                }
            }

            _topTools.Margin = default;
            _tools.Margin = default;
            _bottomTools.Margin = default;
            UpdateDividerVisibility();

            if (_dragGhost != null && _layer != null)
            {
                _layer?.Children.Remove(_dragGhost);
                _layer = null;
                _dragGhost = null;
            }
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("EdgeBarButton"))
        {
            SetGridHitTestVisible(false);
            if (_divider != null)
            {
                _divider.IsVisible = true;
            }

            _dragGhost = new EdgeBarButton { IsChecked = true, IsHitTestVisible = false, Opacity = 0.8 };
            _layer = AdornerLayer.GetAdornerLayer(this);
            _layer?.Children.Add(_dragGhost);

            _topTools.GetRealizedContainers()
                .Concat(_tools.GetRealizedContainers())
                .Concat(_bottomTools.GetRealizedContainers())
                .OfType<ContentPresenter>()
                .Select(i => i.Child)
                .Where(i => i is not null)
                .ToObservable()
                .Subscribe(i => ToolTip.SetServiceEnabled(i, false));

            OnDragOver(sender, e);
        }
    }

    private (DockAreaLocation, int) DetermineLocation(PixelPoint position)
    {
        if (_topTools == null || _tools == null || _bottomTools == null)
            return (default, -1);

        Point clientPosition;

        for (int i = 0; i < _topTools.ItemCount; i++)
        {
            Control? item = _topTools.ContainerFromIndex(i);
            if (item?.IsVisible != true) continue;

            clientPosition = item.PointToClient(position);
            if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2)
            {
                return (DockAreaLocation.Top, i);
            }
        }

        clientPosition = _topTools.PointToClient(position);
        if (clientPosition.Y < _topTools.Bounds.Height + 8)
        {
            return (DockAreaLocation.Top, _topTools.ItemCount);
        }

        for (int i = 0; i < _tools.ItemCount; i++)
        {
            Control? item = _tools.ContainerFromIndex(i);
            if (item?.IsVisible != true) continue;

            clientPosition = item.PointToClient(position);
            if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2)
            {
                return (default, i);
            }
        }

        clientPosition = _tools.PointToClient(position);
        if (clientPosition.Y < _tools.Bounds.Height + 8)
        {
            return (default, _tools.ItemCount);
        }

        for (int i = _bottomTools.ItemCount - 1; i >= 0; i--)
        {
            Control? item = _bottomTools.ContainerFromIndex(i);
            if (item?.IsVisible != true) continue;

            clientPosition = item.PointToClient(position);
            if (clientPosition.Y > item.Bounds.Height / 2)
            {
                return (DockAreaLocation.Bottom, i);
            }
        }


        clientPosition = _bottomTools.PointToClient(position);
        if (clientPosition.Y < _bottomTools.Bounds.Height + 8)
        {
            return (DockAreaLocation.Bottom, 0);
        }

        return (default, -1);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (_topTools == null || _tools == null || _bottomTools == null || _grid == null) return;
        if (e.Data.Contains("EdgeBarButton"))
        {
            _grid.IsHitTestVisible = false;
            var position = this.PointToScreen(e.GetPosition(this));
            Point clientPosition;
            bool handled = false;
            double pad = 0;

            int topToolsVisibleItemsCount = 0;
            for (int i = 0; i < _topTools.ItemCount; i++)
            {
                Control? item = _topTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                topToolsVisibleItemsCount++;

                clientPosition = item.PointToClient(position);
                if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2 && !handled)
                {
                    var ghostPos = _layer.PointToClient(item.PointToScreen(new(0, -pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y - item.Margin.Top, 0, 0);
                    item.Margin = new Thickness(0, 40, 0, 0);
                    handled = true;
                    pad = 0;
                }
                else
                {
                    pad += item.Margin.Top;
                    item.Margin = default;
                }
            }

            clientPosition = _topTools.PointToClient(position);
            if (clientPosition.Y < _topTools.Bounds.Height + 8 && !handled)
            {
                if (topToolsVisibleItemsCount == 0)
                {
                    var ghostPos = _layer.PointToClient(_topTools.PointToScreen(new(0, _topTools.Bounds.Height - pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                    _topTools.Margin = new Thickness(0, 0, 0, 32);
                }
                else
                {
                    var ghostPos =
                        _layer.PointToClient(_topTools.PointToScreen(new(0, _topTools.Bounds.Height + 8 - pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                    _topTools.Margin = new Thickness(0, 0, 0, 40);
                }

                handled = true;
            }
            else
            {
                pad = _topTools.Margin.Bottom;
                _topTools.Margin = default;
            }

            int toolsVisibleItemsCount = 0;
            for (int i = 0; i < _tools.ItemCount; i++)
            {
                Control? item = _tools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                toolsVisibleItemsCount++;

                clientPosition = item.PointToClient(position);
                if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2 && !handled)
                {
                    var ghostPos = _layer.PointToClient(item.PointToScreen(new(0, -pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y - item.Margin.Top, 0, 0);
                    item.Margin = new Thickness(0, 40, 0, 0);
                    handled = true;
                    pad = 0;
                }
                else
                {
                    pad += item.Margin.Top;
                    item.Margin = default;
                }
            }

            clientPosition = _tools.PointToClient(position);
            if (clientPosition.Y < _tools.Bounds.Height + 8 && !handled)
            {
                if (toolsVisibleItemsCount == 0)
                {
                    var ghostPos = _layer.PointToClient(_tools.PointToScreen(new(0, _tools.Bounds.Height - pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                    _tools.Margin = new Thickness(0, 0, 0, 32);
                }
                else
                {
                    var ghostPos = _layer.PointToClient(_tools.PointToScreen(new(0, _tools.Bounds.Height + 8 - pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                    _tools.Margin = new Thickness(0, 0, 0, 40);
                }

                handled = true;
            }
            else
            {
                _tools.Margin = default;
            }

            pad = 0;
            int bottomToolsVisibleItemsCount = 0;
            for (int i = _bottomTools.ItemCount - 1; i >= 0; i--)
            {
                Control? item = _bottomTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                bottomToolsVisibleItemsCount++;

                clientPosition = item.PointToClient(position);
                if (clientPosition.Y > item.Bounds.Height / 2 && !handled)
                {
                    var ghostPos = _layer.PointToClient(item.PointToScreen(new(0, item.Bounds.Height + 8 + pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y - (40 - item.Margin.Bottom), 0, 0);
                    item.Margin = new Thickness(0, 0, 0, 40);
                    handled = true;
                }
                else
                {
                    pad += item.Margin.Bottom;
                    item.Margin = default;
                }
            }


            clientPosition = _bottomTools.PointToClient(position);
            if (clientPosition.Y < _bottomTools.Bounds.Height + 8 && !handled)
            {
                if (bottomToolsVisibleItemsCount == 0)
                {
                    var ghostPos =
                        _layer.PointToClient(_bottomTools.PointToScreen(new(0, pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y - _dragGhost.Bounds.Height, 0, 0);
                    _bottomTools.Margin = new Thickness(0, 32, 0, 0);
                }
                else
                {
                    var ghostPos =
                        _layer.PointToClient(_bottomTools.PointToScreen(new(0, -8 + pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y - _dragGhost.Bounds.Height, 0, 0);
                    _bottomTools.Margin = new Thickness(0, 40, 0, 0);
                }

                handled = true;
            }
            else
            {
                _bottomTools.Margin = default;
            }
        }
    }
}