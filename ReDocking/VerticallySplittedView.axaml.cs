using System;
using System.Linq;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Xaml.Interactivity;

namespace ReDocking;

public class VerticallySplittedView : TemplatedControl, IDockAreaView
{
    public static readonly StyledProperty<object?> TopContentProperty =
        AvaloniaProperty.Register<VerticallySplittedView, object?>(nameof(TopContent));

    public static readonly StyledProperty<IDataTemplate?> TopContentTemplateProperty =
        AvaloniaProperty.Register<VerticallySplittedView, IDataTemplate?>(nameof(TopContentTemplate));

    public static readonly StyledProperty<double> TopHeightProportionProperty =
        AvaloniaProperty.Register<VerticallySplittedView, double>(nameof(TopHeightProportion), defaultValue: 1);

    public static readonly StyledProperty<object?> BottomContentProperty =
        AvaloniaProperty.Register<VerticallySplittedView, object?>(nameof(BottomContent));

    public static readonly StyledProperty<IDataTemplate?> BottomContentTemplateProperty =
        AvaloniaProperty.Register<VerticallySplittedView, IDataTemplate?>(nameof(BottomContentTemplate));

    public static readonly StyledProperty<double> BottomHeightProportionProperty =
        AvaloniaProperty.Register<VerticallySplittedView, double>(nameof(BottomHeightProportion),
            defaultValue: 1);

    private DockArea? _topDockArea;
    private DockArea? _bottomDockArea;
    private ContentPresenter? _topPresenter;
    private ContentPresenter? _bottomPresenter;
    private Thumb? _thumb;
    private Panel? _root;

    private bool _dragEventSubscribed;

    private const double ThumbPadding = 2;

    [DependsOn(nameof(TopContentTemplate))]
    public object? TopContent
    {
        get => GetValue(TopContentProperty);
        set => SetValue(TopContentProperty, value);
    }

    public IDataTemplate? TopContentTemplate
    {
        get => GetValue(TopContentTemplateProperty);
        set => SetValue(TopContentTemplateProperty, value);
    }

    public double TopHeightProportion
    {
        get => GetValue(TopHeightProportionProperty);
        set => SetValue(TopHeightProportionProperty, value);
    }

    [DependsOn(nameof(BottomContentTemplate))]
    public object? BottomContent
    {
        get => GetValue(BottomContentProperty);
        set => SetValue(BottomContentProperty, value);
    }

    public IDataTemplate? BottomContentTemplate
    {
        get => GetValue(BottomContentTemplateProperty);
        set => SetValue(BottomContentTemplateProperty, value);
    }

    public double BottomHeightProportion
    {
        get => GetValue(BottomHeightProportionProperty);
        set => SetValue(BottomHeightProportionProperty, value);
    }

    (DockArea, Control)[] IDockAreaView.GetArea()
    {
        return [(_topDockArea!, _topPresenter!), (_bottomDockArea!,_bottomPresenter!)];
    }

    void IDockAreaView.OnAttachedToDockArea(DockArea dockArea)
    {
        if (dockArea.Location.HasFlag(DockAreaLocation.Top))
        {
            _topDockArea = dockArea;
        }
        else if (dockArea.Location.HasFlag(DockAreaLocation.Bottom))
        {
            _bottomDockArea = dockArea;
        }

        UpdateIsDragDropEnabled();
    }

    void IDockAreaView.OnDetachedFromDockArea(DockArea dockArea)
    {
        if (dockArea.Location.HasFlag(DockAreaLocation.Top))
        {
            _topDockArea = null;
        }
        else if (dockArea.Location.HasFlag(DockAreaLocation.Bottom))
        {
            _bottomDockArea = null;
        }

        UpdateIsDragDropEnabled();
    }

    private void UpdateIsDragDropEnabled()
    {
        var list = Interaction.GetBehaviors(this);
        if (_topDockArea != null || _bottomDockArea != null)
        {
            if (_dragEventSubscribed) return;
            _dragEventSubscribed = true;
            list.Add(new DockAreaDragDropBehavior());
        }
        else
        {
            if (!_dragEventSubscribed) return;
            _dragEventSubscribed = false;

            list.RemoveAll(list.OfType<DockAreaDragDropBehavior>());
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TopContentProperty ||
            change.Property == BottomContentProperty)
        {
            ContentChanged(change);
        }
        else if (change.Property == TopHeightProportionProperty ||
                 change.Property == BottomHeightProportionProperty)
        {
            UpdateSize(Bounds.Size);
        }
    }

    private void ContentChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is ILogical oldChild)
        {
            LogicalChildren.Remove(oldChild);
        }

        if (e.NewValue is ILogical newChild)
        {
            LogicalChildren.Add(newChild);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _topPresenter = e.NameScope.Get<ContentPresenter>("PART_TopContentPresenter");
        _bottomPresenter = e.NameScope.Get<ContentPresenter>("PART_BottomContentPresenter");
        _root = e.NameScope.Get<Panel>("PART_Root");
        _thumb = e.NameScope.Get<Thumb>("PART_Thumb");

        var topVisibilityObservable = _topPresenter.IsChildVisibleObservable();
        var bottomVisibilityObservable = _bottomPresenter.IsChildVisibleObservable();

        _thumb.Bind(IsVisibleProperty, topVisibilityObservable
            .CombineLatest(bottomVisibilityObservable, (left, right) => left && right));

        topVisibilityObservable.CombineLatest(bottomVisibilityObservable)
            .Do(_ => UpdateSize(Bounds.Size))
            .Subscribe(t => IsVisible = t.Item1 || t.Item2);

        _thumb.DragDelta += OnThumbDragDelta;
    }

    private void OnThumbDragDelta(object? sender, VectorEventArgs e)
    {
        if (_topPresenter == null || _bottomPresenter == null || _root == null || _thumb == null)
            return;

        var topHeight = _topPresenter.Height;
        var bottomHeight = _bottomPresenter.Height;
        var delta = e.Vector.Y;
        var size = Bounds.Size;

        var newHeight = topHeight + delta;
        if (newHeight + 5 >= size.Height || newHeight <= 5)
            return;

        var topHeightProportion = newHeight / size.Height;
        var bottomHeightProportion = 1 - topHeightProportion;
        TopHeightProportion = Math.Clamp(topHeightProportion, 0, 1);
        BottomHeightProportion = Math.Clamp(bottomHeightProportion, 0, 1);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateSize(e.NewSize);
    }

    private void UpdateSize(Size size)
    {
        if (_topPresenter == null || _bottomPresenter == null || _thumb == null)
            return;
        (double topHeight, double bottomHeight) = GetAbsoluteHeight(size);

        if (_topPresenter.IsChildVisible() && _bottomPresenter.IsChildVisible())
        {
            _topPresenter.Margin = new Thickness(0, 0, 0, 0);
            _topPresenter.Height = topHeight;

            _thumb.Margin = new Thickness(0, topHeight - ThumbPadding, 0, 0);

            _bottomPresenter.Margin = new Thickness(0, topHeight + ThumbPadding, 0, 0);
            _bottomPresenter.Height = bottomHeight - ThumbPadding;
        }
        else
        {
            if (_topPresenter.IsChildVisible())
            {
                _topPresenter.Margin = new Thickness(0, 0, 0, 0);
                _topPresenter.Height = size.Height;
            }
            else if (_bottomPresenter.IsChildVisible())
            {
                _bottomPresenter.Margin = new Thickness(0, 0, 0, 0);
                _bottomPresenter.Height = size.Height;
            }
        }
    }

    private (double, double) GetAbsoluteHeight(Size availableSize)
    {
        var den = TopHeightProportion + BottomHeightProportion;
        return (availableSize.Height * TopHeightProportion / den,
            availableSize.Height * BottomHeightProportion / den);
    }
}