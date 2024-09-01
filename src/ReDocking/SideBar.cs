using System;
using System.Collections;
using System.Diagnostics;
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

public class SideBar : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable> UpperTopToolsSourceProperty =
        AvaloniaProperty.Register<SideBar, IEnumerable>(nameof(UpperTopToolsSource));

    public static readonly StyledProperty<IEnumerable> UpperBottomToolsSourceProperty =
        AvaloniaProperty.Register<SideBar, IEnumerable>(nameof(UpperBottomToolsSource));

    public static readonly StyledProperty<IEnumerable> LowerTopToolsSourceProperty =
        AvaloniaProperty.Register<SideBar, IEnumerable>(nameof(LowerTopToolsSource));

    public static readonly StyledProperty<IEnumerable> LowerBottomToolsSourceProperty =
        AvaloniaProperty.Register<SideBar, IEnumerable>(nameof(LowerBottomToolsSource));

    public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
        AvaloniaProperty.Register<SideBar, IDataTemplate>(nameof(ItemTemplate));

    public static readonly StyledProperty<SideBarLocation> LocationProperty =
        AvaloniaProperty.Register<SideBar, SideBarLocation>(nameof(Location));

    private ItemsControl? _upperTopTools;
    private ItemsControl? _upperBottomTools;
    private ItemsControl? _lowerTopTools;
    private ItemsControl? _lowerBottomTools;
    private Grid? _grid;
    private StackPanel? _upperStack;
    private StackPanel? _lowerStack;
    private Border? _upperDivider;
    private Border? _lowerDivider;

    private SideBarButton? _dragGhost;
    private AdornerLayer? _layer;

    public SideBar()
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

    public IEnumerable UpperTopToolsSource
    {
        get => GetValue(UpperTopToolsSourceProperty);
        set => SetValue(UpperTopToolsSourceProperty, value);
    }

    public IEnumerable UpperBottomToolsSource
    {
        get => GetValue(UpperBottomToolsSourceProperty);
        set => SetValue(UpperBottomToolsSourceProperty, value);
    }

    public IEnumerable LowerTopToolsSource
    {
        get => GetValue(LowerTopToolsSourceProperty);
        set => SetValue(LowerTopToolsSourceProperty, value);
    }

    public IEnumerable LowerBottomToolsSource
    {
        get => GetValue(LowerBottomToolsSourceProperty);
        set => SetValue(LowerBottomToolsSourceProperty, value);
    }

    public SideBarLocation Location
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
        _upperTopTools = e.NameScope.Get<ItemsControl>("PART_UpperTopTools");
        _upperBottomTools = e.NameScope.Get<ItemsControl>("PART_UpperBottomTools");
        _lowerTopTools = e.NameScope.Get<ItemsControl>("PART_LowerTopTools");
        _lowerBottomTools = e.NameScope.Get<ItemsControl>("PART_LowerBottomTools");
        _grid = e.NameScope.Get<Grid>("PART_Grid");
        _upperStack = e.NameScope.Get<StackPanel>("PART_UpperStack");
        _lowerStack = e.NameScope.Get<StackPanel>("PART_LowerStack");
        _upperDivider = e.NameScope.Get<Border>("PART_UpperDivider");
        _lowerDivider = e.NameScope.Get<Border>("PART_LowerDivider");
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var position = this.PointToScreen(e.GetPosition(this));
        (SideBarButtonLocation location, int index) = DetermineLocation(position);
        OnDragLeave(sender, e);

        SideBar? oldSideBar = null;
        try
        {
            if (!e.Data.Contains("SideBarButton") ||
                e.Data.Get("SideBarButton") is not SideBarButton { DockLocation: not null } button) return;

            if (index < 0) return;

            oldSideBar = button.FindAncestorOfType<SideBar>();
            if (oldSideBar == null) return;

            var args = new SideBarButtonMoveEventArgs(ReDockHost.ButtonMoveEvent, this)
            {
                Item = button.DataContext,
                Button = button,
                SourceSideBar = oldSideBar,
                SourceLocation = button.DockLocation,
                DestinationSideBar = this,
                DestinationLocation = new(location, Location),
                DestinationIndex = index
            };
            RaiseEvent(args);

            if (args.Handled) return;

            var newItemsSource = location switch
            {
                SideBarButtonLocation.UpperTop => UpperTopToolsSource,
                SideBarButtonLocation.UpperBottom => UpperBottomToolsSource,
                SideBarButtonLocation.LowerTop => LowerTopToolsSource,
                SideBarButtonLocation.LowerBottom => LowerBottomToolsSource,
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
        }
        finally
        {
            UpdateDividerVisibility();
            oldSideBar?.UpdateDividerVisibility();
        }
    }

    private void UpdateDividerVisibility()
    {
        if (_upperDivider != null && _upperTopTools != null && _upperBottomTools != null)
        {
            _upperDivider.IsVisible = UpperTopToolsSource.Cast<object>().Any() &&
                                      UpperBottomToolsSource.Cast<object>().Any();
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (_upperTopTools == null || _upperBottomTools == null ||
            _lowerBottomTools == null || _lowerTopTools == null) return;
        if (e.Data.Contains("SideBarButton"))
        {
            SetGridHitTestVisible(true);
            foreach (Control item in _upperTopTools.GetRealizedContainers()
                         .Concat(_upperBottomTools.GetRealizedContainers())
                         .Concat(_lowerBottomTools.GetRealizedContainers())
                         .Concat(_lowerTopTools.GetRealizedContainers()))
            {
                item.Margin = default;
            }

            _upperTopTools.Margin = default;
            _upperBottomTools.Margin = default;
            _lowerTopTools.Margin = default;
            _lowerBottomTools.Margin = default;
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
        if (e.Data.Contains("SideBarButton"))
        {
            SetGridHitTestVisible(false);
            if (_upperDivider != null)
            {
                _upperDivider.IsVisible = true;
            }

            if (_lowerDivider != null)
            {
                _lowerDivider.IsVisible = true;
            }

            _dragGhost = new SideBarButton { IsChecked = true, IsHitTestVisible = false, Opacity = 0.8 };
            _layer = AdornerLayer.GetAdornerLayer(this);
            _layer?.Children.Add(_dragGhost);

            OnDragOver(sender, e);
        }
    }

    private (SideBarButtonLocation, int) DetermineLocation(PixelPoint position)
    {
        if (_upperTopTools == null || _upperBottomTools == null ||
            _lowerTopTools == null || _lowerBottomTools == null || _grid == null ||
            _upperStack == null || _lowerStack == null)
            return (default, -1);

        Point clientPosition;

        for (int i = 0; i < _upperTopTools.ItemCount; i++)
        {
            Control? item = _upperTopTools.ContainerFromIndex(i);
            if (item?.IsVisible != true) continue;

            clientPosition = item.PointToClient(position);
            if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2)
            {
                return (SideBarButtonLocation.UpperTop, i);
            }
        }

        clientPosition = _upperTopTools.PointToClient(position);
        if (clientPosition.Y < _upperTopTools.Bounds.Height + 8)
        {
            return (SideBarButtonLocation.UpperTop, _upperTopTools.ItemCount);
        }

        for (int i = 0; i < _upperBottomTools.ItemCount; i++)
        {
            Control? item = _upperBottomTools.ContainerFromIndex(i);
            if (item?.IsVisible != true) continue;

            clientPosition = item.PointToClient(position);
            if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2)
            {
                return (SideBarButtonLocation.UpperBottom, i);
            }
        }

        // 上のツールと下のツールの間のスペース
        var spaceBetween = _grid.Bounds.Height - (_upperStack.Bounds.Height + _lowerStack.Bounds.Height);
        if (spaceBetween < 0)
        {
            spaceBetween = 16;
        }

        clientPosition = _upperBottomTools.PointToClient(position);
        if (clientPosition.Y < _upperBottomTools.Bounds.Height + spaceBetween / 2)
        {
            return (SideBarButtonLocation.UpperBottom, _upperBottomTools.ItemCount);
        }

        clientPosition = _lowerTopTools.PointToClient(position);
        if (clientPosition.Y < 0)
        {
            return (SideBarButtonLocation.LowerTop, 0);
        }

        for (int i = 0; i < _lowerTopTools.ItemCount; i++)
        {
            Control? item = _lowerTopTools.ContainerFromIndex(i);
            if (item?.IsVisible != true) continue;

            clientPosition = item.PointToClient(position);
            if (clientPosition.Y < item.Bounds.Height / 2)
            {
                return (SideBarButtonLocation.LowerTop, i);
            }
        }

        clientPosition = _lowerTopTools.PointToClient(position);
        if (clientPosition.Y < _lowerTopTools.Bounds.Height - 16)
        {
            return (SideBarButtonLocation.LowerTop, _lowerTopTools.ItemCount);
        }

        clientPosition = _lowerBottomTools.PointToClient(position);
        if (clientPosition.Y < -8)
        {
            return (SideBarButtonLocation.LowerBottom, 0);
        }

        for (int i = 0; i < _lowerBottomTools.ItemCount; i++)
        {
            Control? item = _lowerBottomTools.ContainerFromIndex(i);
            if (item?.IsVisible != true) continue;

            clientPosition = item.PointToClient(position);
            if (clientPosition.Y < item.Bounds.Height / 2)
            {
                return (SideBarButtonLocation.LowerBottom, i);
            }
        }

        clientPosition = _lowerBottomTools.PointToClient(position);
        if (clientPosition.Y > _lowerBottomTools.Bounds.Height - 16)
        {
            return (SideBarButtonLocation.LowerBottom, _lowerBottomTools.ItemCount);
        }
        
        return (default, -1);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (_upperTopTools == null || _upperBottomTools == null || _lowerTopTools == null ||
            _lowerBottomTools == null || _grid == null ||
            _upperStack == null || _lowerStack == null ||
            _layer == null || _dragGhost == null) return;
        if (e.Data.Contains("SideBarButton"))
        {
            const double Spacing = 8;
            const double Size = 32;

            _grid.IsHitTestVisible = false;
            var position = this.PointToScreen(e.GetPosition(this));
            Point clientPosition;
            bool handled = false;
            double pad = 0;

            int upperTopToolsVisibleItemsCount = 0;
            for (int i = 0; i < _upperTopTools.ItemCount; i++)
            {
                Control? item = _upperTopTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                upperTopToolsVisibleItemsCount++;

                clientPosition = item.PointToClient(position);
                if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2 && !handled)
                {
                    var ghostPos = _layer.PointToClient(item.PointToScreen(new(0, -pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y - item.Margin.Top, 0, 0);
                    item.Margin = new Thickness(0, Size + Spacing, 0, 0);
                    handled = true;
                    pad = 0;
                }
                else
                {
                    pad += item.Margin.Top;
                    item.Margin = default;
                }
            }

            clientPosition = _upperTopTools.PointToClient(position);
            if (clientPosition.Y < _upperTopTools.Bounds.Height + Spacing && !handled)
            {
                if (upperTopToolsVisibleItemsCount == 0)
                {
                    var ghostPos =
                        _layer.PointToClient(_upperTopTools.PointToScreen(new(0, _upperTopTools.Bounds.Height - pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                    _upperTopTools.Margin = new Thickness(0, 0, 0, Size);
                }
                else
                {
                    var ghostPos =
                        _layer.PointToClient(
                            _upperTopTools.PointToScreen(new(0, _upperTopTools.Bounds.Height + Spacing - pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                    _upperTopTools.Margin = new Thickness(0, 0, 0, Size + Spacing);
                }

                handled = true;
            }
            else
            {
                pad = _upperTopTools.Margin.Bottom;
                _upperTopTools.Margin = default;
            }

            int upperBottomToolsVisibleItemsCount = 0;
            for (int i = 0; i < _upperBottomTools.ItemCount; i++)
            {
                Control? item = _upperBottomTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                upperBottomToolsVisibleItemsCount++;

                clientPosition = item.PointToClient(position);
                if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2 && !handled)
                {
                    var ghostPos = _layer.PointToClient(item.PointToScreen(new(0, -pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y - item.Margin.Top, 0, 0);
                    item.Margin = new Thickness(0, Size + Spacing, 0, 0);
                    handled = true;
                    pad = 0;
                }
                else
                {
                    pad += item.Margin.Top;
                    item.Margin = default;
                }
            }

            // 上のツールと下のツールの間のスペース
            var spaceBetween = _grid.Bounds.Height - (_upperStack.Bounds.Height + _lowerStack.Bounds.Height);
            if (spaceBetween < 0)
            {
                spaceBetween = 16;
            }

            clientPosition = _upperBottomTools.PointToClient(position);
            if (clientPosition.Y < _upperBottomTools.Bounds.Height + spaceBetween / 2 && !handled)
            {
                if (upperBottomToolsVisibleItemsCount == 0)
                {
                    var ghostPos =
                        _layer.PointToClient(
                            _upperBottomTools.PointToScreen(new(0, _upperBottomTools.Bounds.Height - pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                }
                else
                {
                    var ghostPos =
                        _layer.PointToClient(
                            _upperBottomTools.PointToScreen(new(0, _upperBottomTools.Bounds.Height + Spacing - pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                }

                handled = true;
            }

            pad = 0;
            int lowerBottomToolsVisibleItemsCount = 0;
            for (int i = _lowerBottomTools.ItemCount - 1; i >= 0; i--)
            {
                Control? item = _lowerBottomTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                lowerBottomToolsVisibleItemsCount++;

                clientPosition = item.PointToClient(position);
                if (clientPosition.Y > item.Bounds.Height / 2 && !handled)
                {
                    var ghostPos = _layer.PointToClient(item.PointToScreen(new(0, pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y + item.Margin.Bottom, 0, 0);
                    item.Margin = new Thickness(0, 0, 0, Size + Spacing);
                    handled = true;
                }
                else
                {
                    pad += item.Margin.Bottom;
                    item.Margin = default;
                }
            }

            clientPosition = _lowerBottomTools.PointToClient(position);
            if (clientPosition.Y > -8 && !handled)
            {
                if (lowerBottomToolsVisibleItemsCount == 0)
                {
                    var ghostPos =
                        _layer.PointToClient(
                            _lowerTopTools.PointToScreen(new(0, (Spacing * 2) + pad + _lowerTopTools.Bounds.Height)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                    _lowerBottomTools.Margin = new Thickness(0, Size, 0, 0);
                }
                else
                {
                    var ghostPos =
                        _layer.PointToClient(_lowerBottomTools.PointToScreen(new(0, -(Size + Spacing) + pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y, 0, 0);
                    _lowerBottomTools.Margin = new Thickness(0, Size + Spacing, 0, 0);
                }

                handled = true;
            }
            else
            {
                pad = _lowerBottomTools.Margin.Top;
                _lowerBottomTools.Margin = default;
            }

            int lowerTopToolsVisibleItemsCount = 0;
            for (int i = _lowerTopTools.ItemCount - 1; i >= 0; i--)
            {
                Control? item = _lowerTopTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                lowerTopToolsVisibleItemsCount++;

                clientPosition = item.PointToClient(position);
                if (clientPosition.Y > item.Bounds.Height / 2 && !handled)
                {
                    var ghostPos = _layer.PointToClient(item.PointToScreen(new(0, pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y + item.Margin.Bottom, 0, 0);
                    item.Margin = new Thickness(0, 0, 0, Size + Spacing);
                    handled = true;
                }
                else
                {
                    pad += item.Margin.Bottom;
                    item.Margin = default;
                }
            }

            clientPosition = _lowerTopTools.PointToClient(position);
            if (clientPosition.Y < _lowerTopTools.Bounds.Height + spaceBetween / 2 && !handled)
            {
                if (lowerTopToolsVisibleItemsCount == 0)
                {
                    var ghostPos =
                        _layer.PointToClient(_lowerTopTools.PointToScreen(new(0, pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y - _dragGhost.Bounds.Height, 0, 0);
                }
                else
                {
                    var ghostPos =
                        _layer.PointToClient(_lowerTopTools.PointToScreen(new(0, -8 + pad)));
                    _dragGhost.Margin = new(ghostPos.X, ghostPos.Y - _dragGhost.Bounds.Height, 0, 0);
                }
            }
        }
    }
}