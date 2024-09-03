using System.Collections;
using System.Collections.Specialized;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
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
    private ReDockHost? _host;

    private SideBarButtonLocation[]? _supportedLocations;
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

        UpdateDividerVisibility();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == UpperTopToolsSourceProperty ||
            change.Property == UpperBottomToolsSourceProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= OnUpperToolsCollectionChanged;

            if (change.NewValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += OnUpperToolsCollectionChanged;

            UpdateUpperDividerVisibility();
        }
        else if (change.Property == LowerTopToolsSourceProperty ||
                 change.Property == LowerBottomToolsSourceProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= OnLowerToolsCollectionChanged;

            if (change.NewValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += OnLowerToolsCollectionChanged;

            UpdateLowerDividerVisibility();
        }
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        _host = this.FindLogicalAncestorOfType<ReDockHost>();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _host = null;
    }

    private void OnLowerToolsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateLowerDividerVisibility();
    }

    private void OnUpperToolsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateUpperDividerVisibility();
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        (SideBarButtonLocation location, int index) = DetermineLocation(e);
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
                _ => null
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
        UpdateUpperDividerVisibility();
        UpdateLowerDividerVisibility();
    }

    private void UpdateUpperDividerVisibility()
    {
        if (_upperDivider != null && _upperTopTools != null && _upperBottomTools != null)
        {
            // 片方しか対応しない場合、Dividerは表示しない
            if (SupportsLocation(SideBarButtonLocation.UpperTop) && SupportsLocation(SideBarButtonLocation.UpperBottom))
            {
                _upperDivider.IsVisible = UpperTopToolsSource.Cast<object>().Any() &&
                                          UpperBottomToolsSource.Cast<object>().Any();
            }
            else
            {
                _upperDivider.IsVisible = false;
            }
        }
    }

    private void UpdateLowerDividerVisibility()
    {
        if (_lowerDivider != null && _lowerTopTools != null && _lowerBottomTools != null)
        {
            // 片方しか対応しない場合、Dividerは表示しない
            if (SupportsLocation(SideBarButtonLocation.LowerTop) && SupportsLocation(SideBarButtonLocation.LowerBottom))
            {
                _lowerDivider.IsVisible = LowerTopToolsSource.Cast<object>().Any() &&
                                          LowerBottomToolsSource.Cast<object>().Any();
            }
            else
            {
                _lowerDivider.IsVisible = false;
            }
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
            _supportedLocations = _host?.DockAreas
                .Where(i => i.Location.LeftRight == Location)
                .Select(i => i.Location.ButtonLocation)
                .ToArray();

            if (_upperDivider != null)
            {
                _upperDivider.IsVisible = SupportsLocation(SideBarButtonLocation.UpperTop) &&
                                          SupportsLocation(SideBarButtonLocation.UpperBottom);
            }

            if (_lowerDivider != null)
            {
                _lowerDivider.IsVisible = SupportsLocation(SideBarButtonLocation.LowerTop) &&
                                          SupportsLocation(SideBarButtonLocation.LowerBottom);
            }

            _dragGhost = new SideBarButton { IsHitTestVisible = false, Classes = { "ghost" } };
            _layer = AdornerLayer.GetAdornerLayer(this);
            _layer?.Children.Add(_dragGhost);

            OnDragOver(sender, e);
        }
    }

    private (SideBarButtonLocation, int) DetermineLocation(DragEventArgs e)
    {
        if (_upperTopTools == null || _upperBottomTools == null ||
            _lowerTopTools == null || _lowerBottomTools == null || _grid == null ||
            _upperStack == null || _lowerStack == null)
        {
            return (default, -1);
        }

        Point clientPosition;

        // 上のツールと下のツールの間のスペース
        var spaceBetween = _grid.Bounds.Height - (_upperStack.Bounds.Height + _lowerStack.Bounds.Height);
        if (spaceBetween < 0)
        {
            spaceBetween = 16;
        }

        const double Spacing = 8;

        if (SupportsLocation(SideBarButtonLocation.UpperTop))
        {
            for (int i = 0; i < _upperTopTools.ItemCount; i++)
            {
                Control? item = _upperTopTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;

                clientPosition = e.GetPosition(item);
                // ポインターの位置がアイテムの高さの半分より上にある場合
                if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2)
                {
                    return (SideBarButtonLocation.UpperTop, i);
                }
            }

            clientPosition = e.GetPosition(_upperTopTools);
            // ポインターの位置がUpperTopToolsの高さより上にある場合、最後のアイテムとして扱う
            if (clientPosition.Y < _upperTopTools.Bounds.Height + Spacing)
            {
                return (SideBarButtonLocation.UpperTop, _upperTopTools.ItemCount);
            }
        }

        if (SupportsLocation(SideBarButtonLocation.UpperBottom))
        {
            for (int i = 0; i < _upperBottomTools.ItemCount; i++)
            {
                Control? item = _upperBottomTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;

                clientPosition = e.GetPosition(item);
                // ポインターの位置がアイテムの高さの半分より上にある場合
                if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2)
                {
                    return (SideBarButtonLocation.UpperBottom, i);
                }
            }

            clientPosition = e.GetPosition(_upperBottomTools);
            // ポインターの位置がUpperBottomToolsの高さと空白の半分を足した値より上にある場合、最後のアイテムとして扱う
            if (clientPosition.Y < _upperBottomTools.Bounds.Height + spaceBetween / 2)
            {
                return (SideBarButtonLocation.UpperBottom, _upperBottomTools.ItemCount);
            }
        }

        if (SupportsLocation(SideBarButtonLocation.LowerTop))
        {
            // LowerTopからLowerTopに移動する場合、IsVisibleがfalseになり、下のforで引っかからないので、先に処理する
            clientPosition = e.GetPosition(_lowerTopTools);
            // ポインターの位置がLowerTopToolsのY座標より上にある場合、最初のアイテムとして扱う
            if (clientPosition.Y < 0)
            {
                return (SideBarButtonLocation.LowerTop, 0);
            }

            for (int i = 0; i < _lowerTopTools.ItemCount; i++)
            {
                Control? item = _lowerTopTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;

                clientPosition = e.GetPosition(item);
                // ポインターの位置がアイテムの高さの半分より上にある場合
                if (clientPosition.Y < item.Bounds.Height / 2)
                {
                    return (SideBarButtonLocation.LowerTop, i);
                }
            }

            clientPosition = e.GetPosition(_lowerTopTools);
            // ポインターの位置がLowerTopToolsの高さより上にある場合、最後のアイテムとして扱う
            if (clientPosition.Y < _lowerTopTools.Bounds.Height + 8)
            {
                return (SideBarButtonLocation.LowerTop, _lowerTopTools.ItemCount);
            }
        }

        if (SupportsLocation(SideBarButtonLocation.LowerBottom))
        {
            clientPosition = e.GetPosition(_lowerBottomTools);
            // ポインターの位置がLowerBottomToolsのY座標より-8px上にある場合、最初のアイテムとして扱う
            if (clientPosition.Y < -8)
            {
                return (SideBarButtonLocation.LowerBottom, 0);
            }

            for (int i = 0; i < _lowerBottomTools.ItemCount; i++)
            {
                Control? item = _lowerBottomTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;

                clientPosition = e.GetPosition(item);
                if (clientPosition.Y < item.Bounds.Height / 2)
                {
                    return (SideBarButtonLocation.LowerBottom, i);
                }
            }

            clientPosition = e.GetPosition(_lowerBottomTools);
            if (clientPosition.Y > _lowerBottomTools.Bounds.Height - 16)
            {
                return (SideBarButtonLocation.LowerBottom, _lowerBottomTools.ItemCount);
            }
        }

        return (default, -1);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (_upperTopTools == null || _upperBottomTools == null || _lowerTopTools == null ||
            _lowerBottomTools == null || _grid == null ||
            _upperStack == null || _lowerStack == null ||
            _layer == null || _dragGhost == null)
        {
            return;
        }

        if (!e.Data.Contains("SideBarButton"))
        {
            return;
        }

        const double Spacing = 8;
        const double Size = 32;

        _grid.IsHitTestVisible = false;
        Point clientPosition;
        bool handled = false;
        double pad = 0;

        // 上のツールと下のツールの間のスペース
        var spaceBetween = _grid.Bounds.Height - (_upperStack.Bounds.Height + _lowerStack.Bounds.Height);
        if (spaceBetween < 0)
        {
            spaceBetween = 16;
        }

        if (SupportsLocation(SideBarButtonLocation.UpperTop))
        {
            int upperTopToolsVisibleItemsCount = 0;
            for (int i = 0; i < _upperTopTools.ItemCount; i++)
            {
                Control? item = _upperTopTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                upperTopToolsVisibleItemsCount++;

                clientPosition = e.GetPosition(item);
                if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2 && !handled)
                {
                    SetGhostYPosition(item, -pad - item.Margin.Top);
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

            clientPosition = e.GetPosition(_upperTopTools);
            if (clientPosition.Y < _upperTopTools.Bounds.Height + Spacing && !handled)
            {
                if (upperTopToolsVisibleItemsCount == 0)
                {
                    SetGhostYPosition(_upperTopTools, _upperTopTools.Bounds.Height - pad);
                    _upperTopTools.Margin = new Thickness(0, 0, 0, Size);
                }
                else
                {
                    SetGhostYPosition(_upperTopTools, _upperTopTools.Bounds.Height + Spacing - pad);
                    _upperTopTools.Margin = new Thickness(0, 0, 0, Size + Spacing);
                }

                handled = true;
            }
            else
            {
                pad = _upperTopTools.Margin.Bottom;
                _upperTopTools.Margin = default;
            }
        }

        if (SupportsLocation(SideBarButtonLocation.UpperBottom))
        {
            int upperBottomToolsVisibleItemsCount = 0;
            for (int i = 0; i < _upperBottomTools.ItemCount; i++)
            {
                Control? item = _upperBottomTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                upperBottomToolsVisibleItemsCount++;

                clientPosition = e.GetPosition(item);
                if (clientPosition.Y + item.Margin.Top < item.Bounds.Height / 2 && !handled)
                {
                    SetGhostYPosition(item, -pad - item.Margin.Top);
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

            clientPosition = e.GetPosition(_upperBottomTools);
            if (clientPosition.Y < _upperBottomTools.Bounds.Height + spaceBetween / 2 && !handled)
            {
                if (upperBottomToolsVisibleItemsCount == 0)
                {
                    SetGhostYPosition(_upperBottomTools, _upperBottomTools.Bounds.Height - pad);
                }
                else
                {
                    SetGhostYPosition(_upperBottomTools, _upperBottomTools.Bounds.Height + Spacing - pad);
                }

                handled = true;
            }
        }

        pad = 0;

        if (SupportsLocation(SideBarButtonLocation.LowerBottom))
        {
            int lowerBottomToolsVisibleItemsCount = 0;
            for (int i = _lowerBottomTools.ItemCount - 1; i >= 0; i--)
            {
                Control? item = _lowerBottomTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                lowerBottomToolsVisibleItemsCount++;

                clientPosition = e.GetPosition(item);
                if (clientPosition.Y > item.Bounds.Height / 2 && !handled)
                {
                    SetGhostYPosition(item, pad + item.Margin.Bottom);
                    item.Margin = new Thickness(0, 0, 0, Size + Spacing);
                    handled = true;
                }
                else
                {
                    pad += item.Margin.Bottom;
                    item.Margin = default;
                }
            }

            clientPosition = e.GetPosition(_lowerBottomTools);
            if (clientPosition.Y > -8 && !handled)
            {
                if (lowerBottomToolsVisibleItemsCount == 0)
                {
                    // _lowerBottomのサイズがよくわからないので、_lowerTopのサイズを使う
                    SetGhostYPosition(_lowerTopTools, (Spacing * 2) + pad + _lowerTopTools.Bounds.Height);
                    _lowerBottomTools.Margin = new Thickness(0, Size, 0, 0);
                }
                else
                {
                    SetGhostYPosition(_lowerBottomTools, -(Size + Spacing) + pad);
                    _lowerBottomTools.Margin = new Thickness(0, Size + Spacing, 0, 0);
                }

                handled = true;
            }
            else
            {
                pad = _lowerBottomTools.Margin.Top;
                _lowerBottomTools.Margin = default;
            }
        }

        if (SupportsLocation(SideBarButtonLocation.LowerTop))
        {
            int lowerTopToolsVisibleItemsCount = 0;
            for (int i = _lowerTopTools.ItemCount - 1; i >= 0; i--)
            {
                Control? item = _lowerTopTools.ContainerFromIndex(i);
                if (item?.IsVisible != true) continue;
                lowerTopToolsVisibleItemsCount++;

                clientPosition = e.GetPosition(item);
                if (clientPosition.Y > item.Bounds.Height / 2 && !handled)
                {
                    SetGhostYPosition(item, pad + item.Margin.Bottom);
                    item.Margin = new Thickness(0, 0, 0, Size + Spacing);
                    handled = true;
                }
                else
                {
                    pad += item.Margin.Bottom;
                    item.Margin = default;
                }
            }

            clientPosition = e.GetPosition(_lowerTopTools);
            if (clientPosition.Y < _lowerTopTools.Bounds.Height + spaceBetween / 2 && !handled)
            {
                if (lowerTopToolsVisibleItemsCount == 0)
                {
                    SetGhostYPosition(_lowerTopTools, pad - _dragGhost.Bounds.Height);
                }
                else
                {
                    SetGhostYPosition(_lowerTopTools, -Spacing + pad - _dragGhost.Bounds.Height);
                }
            }
        }
    }

    private void SetGhostYPosition(Control @base, double y)
    {
        _dragGhost!.Margin = (@base.TranslatePoint(new Point(0, y), _layer!) ?? default).ToThickness();
    }

    private bool SupportsLocation(SideBarButtonLocation location)
    {
        return _supportedLocations?.Contains(location) != false;
    }
}