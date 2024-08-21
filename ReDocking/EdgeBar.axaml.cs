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
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("EdgeBarButton"))
        {
            SetGridHitTestVisible(false);
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

            for (int i = 0; i < _topTools.ItemCount; i++)
            {
                Control? item = _topTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;

                clientPosition = item.PointToClient(position);
                if (clientPosition.Y < item.Bounds.Height / 2 && !handled)
                {
                    item.Margin = new Thickness(0, 40, 0, 0);
                    handled = true;
                }
                else
                {
                    item.Margin = default;
                }
            }

            clientPosition = _topTools.PointToClient(position);
            if (clientPosition.Y < _topTools.Bounds.Height + 8 && !handled)
            {
                _topTools.Margin = new Thickness(0, 0, 0, 40);
                handled = true;
            }
            else
            {
                _topTools.Margin = default;
            }

            for (int i = 0; i < _tools.ItemCount; i++)
            {
                Control? item = _tools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;

                clientPosition = item.PointToClient(position);
                if (clientPosition.Y < item.Bounds.Height / 2 && !handled)
                {
                    item.Margin = new Thickness(0, 40, 0, 0);
                    handled = true;
                }
                else
                {
                    item.Margin = default;
                }
            }


            for (int i = _bottomTools.ItemCount - 1; i >= 0; i--)
            {
                Control? item = _bottomTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;

                clientPosition = item.PointToClient(position);
                if (clientPosition.Y > item.Bounds.Height / 2 && !handled)
                {
                    item.Margin = new Thickness(0, 0, 0, 40);
                    handled = true;
                }
                else
                {
                    item.Margin = default;
                }
            }
        }
    }
}