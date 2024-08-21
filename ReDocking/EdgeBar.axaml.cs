using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Templates;

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

    private ItemsControl? _topTools;
    private ItemsControl? _tools;
    private ItemsControl? _bottomTools;
    private Grid? _grid;

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
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        OnDragLeave(sender, e);
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
            }

            _topTools.Margin = default;

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
            _dragGhost = new EdgeBarButton { IsChecked = true, IsHitTestVisible = false, Opacity = 0.8 };
            _layer = AdornerLayer.GetAdornerLayer(this);
            _layer?.Children.Add(_dragGhost);
        }
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

            for (int i = 0; i < _topTools.ItemCount; i++)
            {
                Control? item = _topTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;

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
                var ghostPos = _layer.PointToClient(_topTools.PointToScreen(new(0, _topTools.Bounds.Height + 8 - pad)));
                _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                _topTools.Margin = new Thickness(0, 0, 0, 40);
                handled = true;
            }
            else
            {
                pad = _topTools.Margin.Bottom;
                _topTools.Margin = default;
            }

            for (int i = 0; i < _tools.ItemCount; i++)
            {
                Control? item = _tools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;

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
                var ghostPos = _layer.PointToClient(_tools.PointToScreen(new(0, _tools.Bounds.Height + 8 - pad)));
                _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                _tools.Margin = new Thickness(0, 0, 0, 40);
                handled = true;
            }
            else
            {
                _tools.Margin = default;
            }

            pad = 0;
            for (int i = _bottomTools.ItemCount - 1; i >= 0; i--)
            {
                Control? item = _bottomTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;

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
                var ghostPos =
                    _layer.PointToClient(_bottomTools.PointToScreen(new(0, -8 + pad)));
                _dragGhost.Margin = new(ghostPos.X, ghostPos.Y - _dragGhost.Bounds.Height, 0, 0);
                _bottomTools.Margin = new Thickness(0, 40, 0, 0);
                handled = true;
            }
            else
            {
                _bottomTools.Margin = default;
            }
        }
    }
}