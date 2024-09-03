using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace ReDocking;

public class OverflowLayoutView : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<OverflowLayoutView, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<OverflowLayoutView, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<IDataTemplate?> MenuItemTemplateProperty =
        AvaloniaProperty.Register<OverflowLayoutView, IDataTemplate?>(nameof(MenuItemTemplate));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<OverflowLayoutView, Orientation>(nameof(Orientation), Orientation.Vertical);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<OverflowLayoutView, double>(nameof(Spacing));

    public static readonly StyledProperty<Button?> ButtonProperty =
        AvaloniaProperty.Register<OverflowLayoutView, Button?>("Button");

    private readonly AvaloniaList<object> _items = [];
    private readonly AvaloniaList<object> _ellipsisItems = [];
    private readonly List<(object, Size)> _sizeCache = [];
    private ItemsControl? _itemsControl;
    private ContentPresenter? _buttonPresenter;
    private Size _buttonSize;

    [Content]
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public IDataTemplate? MenuItemTemplate
    {
        get => GetValue(MenuItemTemplateProperty);
        set => SetValue(MenuItemTemplateProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public Button? Button
    {
        get => GetValue(ButtonProperty);
        set => SetValue(ButtonProperty, value);
    }

    internal ItemsControl? ItemsControl => _itemsControl;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        // _grid = e.NameScope.Get<Grid>("PART_Grid");
        _itemsControl = e.NameScope.Get<ItemsControl>("PART_ItemsControl");
        _buttonPresenter = e.NameScope.Get<ContentPresenter>("PART_ButtonPresenter");
        _itemsControl.ItemsSource = _items;
        _itemsControl.ItemTemplate = ItemTemplate;
        _itemsControl.ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel
        {
            Spacing = Spacing,
            Orientation = Orientation
        });
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            UpdateItemsSource(change.OldValue as IEnumerable, change.NewValue as IEnumerable);
        }

        if (_itemsControl != null)
        {
            if (change.Property == ItemTemplateProperty)
            {
                _itemsControl.ItemTemplate = ItemTemplate;
            }
            else if (_itemsControl.ItemsPanelRoot is StackPanel panel &&
                     (change.Property == SpacingProperty ||
                      change.Property == OrientationProperty))
            {
                panel.Spacing = Spacing;
                panel.Orientation = Orientation;
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_buttonSize.Width == 0 ||
            _buttonSize.Height == 0)
        {
            _buttonPresenter!.IsVisible = true;
            _buttonPresenter.Measure(Size.Infinity);
            _buttonSize = _buttonPresenter.DesiredSize;
            _buttonPresenter.IsVisible = _ellipsisItems.Count > 0;
        }

        if (double.IsInfinity(availableSize.Width))
        {
            ResetItems();
        }
        else
        {
            UpdateLayout(availableSize);
        }

        return base.MeasureOverride(availableSize);
    }

    private void UpdateSizeCache()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            var obj = _items[i];
            var index = _sizeCache.FindIndex(x => x.Item1.Equals(obj));
            var control = _itemsControl!.ContainerFromIndex(i);
            if (control == null)
            {
                _sizeCache.RemoveAt(index);
                continue;
            }

            var size = control.DesiredSize;
            if (index >= 0)
            {
                _sizeCache[index] = (obj, size);
            }
            else
            {
                _sizeCache.Add((obj, size));
            }
        }
    }

    private void UpdateItemsSource(IEnumerable? oldValue, IEnumerable? newValue)
    {
        if (oldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnItemsSourceCollectionChanged;
        }

        if (newValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += OnItemsSourceCollectionChanged;
        }

        _sizeCache.Clear();
        ResetItems();
        InvalidateMeasure();
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var startIndex = e.NewStartingIndex;
            if (startIndex <= _items.Count)
            {
                var items = e.NewItems!;
                _items.AddRange(items.Cast<object>());
                InvalidateMeasure();
                return;
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            var startIndex = e.OldStartingIndex;
            _sizeCache.RemoveAll(o => e.OldItems!.Contains(o.Item1));
            if (startIndex >= _items.Count)
            {
                var count = e.OldItems!.Count;
                _ellipsisItems.RemoveRange(startIndex - _items.Count, count);
                InvalidateMeasure();
                return;
            }
        }

        ResetItems();
        InvalidateMeasure();
    }

    private void ResetItems()
    {
        _items.Clear();
        _ellipsisItems.Clear();
        if (ItemsSource != null)
        {
            _items.AddRange(ItemsSource.Cast<object>());
        }

        if (_buttonPresenter != null)
        {
            _buttonPresenter.IsVisible = false;
        }
    }

    private double GetSize(Size size)
    {
        return Orientation == Orientation.Horizontal ? size.Width : size.Height;
    }

    private double GetItemSize(object item)
    {
        return GetSize(_sizeCache.FirstOrDefault(x => x.Item1.Equals(item)).Item2);
    }

    private double GetButtonSize()
    {
        return GetSize(_buttonSize);
    }

    private void UpdateLayout(Size availableSize)
    {
        var available = GetSize(availableSize);
        var buttonSize = GetButtonSize();
        available -= buttonSize + Spacing;
        var size = GetSize(LayoutHelper.MeasureChild(_itemsControl, Size.Infinity, default));
        UpdateSizeCache();
        if (size >= available)
        {
            // オーバーフローしている
            var overflow = size - available;
            int i = _items.Count - 1;
            for (; i >= 0; i--)
            {
                var item = _items[i];
                overflow -= GetItemSize(item);
                overflow -= Spacing;
                if (overflow < 0)
                {
                    if (overflow - (buttonSize + Spacing) < 0)
                    {
                        i--;
                    }

                    break;
                }
            }

            if (i < _items.Count - 1)
            {
                var newEllipsisItems = _items.Skip(i + 1).ToArray();
                _items.RemoveRange(i + 1, _items.Count - i - 1);
                _ellipsisItems.InsertRange(0, newEllipsisItems);
                _buttonPresenter!.IsVisible = _ellipsisItems.Count > 0;
            }
        }
        else
        {
            // 余白があるので、省略されたアイテムを戻す
            var space = available - size;
            int i = 0;
            for (; i < _ellipsisItems.Count; i++)
            {
                var ellipsisItem = _ellipsisItems[i];
                var length = GetItemSize(ellipsisItem);
                space -= length;
                space -= Spacing;
                if (space < 0)
                {
                    break;
                }
            }

            if (i > 0)
            {
                var newItems = _ellipsisItems.Take(i).ToArray();
                _ellipsisItems.RemoveRange(0, i);
                _items.AddRange(newItems);
                _buttonPresenter!.IsVisible = _ellipsisItems.Count > 0;
            }
        }
    }
}