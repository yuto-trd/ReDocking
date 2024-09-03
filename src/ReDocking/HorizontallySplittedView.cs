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

public class HorizontallySplittedView : TemplatedControl, IDockAreaView
{
    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<HorizontallySplittedView, object?>(nameof(LeftContent));

    public static readonly StyledProperty<IDataTemplate?> LeftContentTemplateProperty =
        AvaloniaProperty.Register<HorizontallySplittedView, IDataTemplate?>(nameof(LeftContentTemplate));

    public static readonly StyledProperty<double> LeftWidthProportionProperty =
        AvaloniaProperty.Register<HorizontallySplittedView, double>(nameof(LeftWidthProportion), defaultValue: 1);

    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<HorizontallySplittedView, object?>(nameof(RightContent));

    public static readonly StyledProperty<IDataTemplate?> RightContentTemplateProperty =
        AvaloniaProperty.Register<HorizontallySplittedView, IDataTemplate?>(nameof(RightContentTemplate));

    public static readonly StyledProperty<double> RightWidthProportionProperty =
        AvaloniaProperty.Register<HorizontallySplittedView, double>(nameof(RightWidthProportion),
            defaultValue: 1);

    private DockArea? _leftDockArea;
    private DockArea? _rightDockArea;
    private ContentPresenter? _leftPresenter;
    private ContentPresenter? _rightPresenter;
    private Thumb? _thumb;
    private Panel? _root;
    private bool _dragEventSubscribed;

    private const double ThumbPadding = 2;

    static HorizontallySplittedView()
    {
        DockAreaDragDropBehavior.BehaviorTypeProperty.Changed.AddClassHandler<HorizontallySplittedView, Type>(
            (s, e) => s.OnBehaviorTypeChanged(e));
    }

    [DependsOn(nameof(LeftContentTemplate))]
    public object? LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public IDataTemplate? LeftContentTemplate
    {
        get => GetValue(LeftContentTemplateProperty);
        set => SetValue(LeftContentTemplateProperty, value);
    }

    public double LeftWidthProportion
    {
        get => GetValue(LeftWidthProportionProperty);
        set => SetValue(LeftWidthProportionProperty, value);
    }

    [DependsOn(nameof(RightContentTemplate))]
    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public IDataTemplate? RightContentTemplate
    {
        get => GetValue(RightContentTemplateProperty);
        set => SetValue(RightContentTemplateProperty, value);
    }

    public double RightWidthProportion
    {
        get => GetValue(RightWidthProportionProperty);
        set => SetValue(RightWidthProportionProperty, value);
    }

    (DockArea, Control)[] IDockAreaView.GetArea()
    {
        return [(_leftDockArea!, _leftPresenter!), (_rightDockArea!, _rightPresenter!)];
    }
    
    void IDockAreaView.OnAttachedToDockArea(DockArea dockArea)
    {
        if (dockArea.Target == nameof(LeftContent))
        {
            _leftDockArea = dockArea;
        }
        else if (dockArea.Target == nameof(RightContent))
        {
            _rightDockArea = dockArea;
        }

        dockArea.PropertyChanged += DockAreaOnPropertyChanged;
        UpdateIsDragDropEnabled();
    }

    private void DockAreaOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == DockArea.TargetProperty)
        {
            if (sender is DockArea dockArea)
            {
                switch (e.OldValue)
                {
                    case nameof(LeftContent):
                        _leftDockArea = null;
                        break;
                    case nameof(RightContent):
                        _rightDockArea = null;
                        break;
                }

                switch (dockArea.Target)
                {
                    case nameof(LeftContent):
                        _leftDockArea = dockArea;
                        break;
                    case nameof(RightContent):
                        _rightDockArea = dockArea;
                        break;
                }
            }

            UpdateIsDragDropEnabled();
        }
    }

    void IDockAreaView.OnDetachedFromDockArea(DockArea dockArea)
    {
        if (dockArea.Target == nameof(LeftContent))
        {
            _leftDockArea = null;
        }
        else if (dockArea.Target == nameof(RightContent))
        {
            _rightDockArea = null;
        }

        dockArea.PropertyChanged -= DockAreaOnPropertyChanged;
        UpdateIsDragDropEnabled();
    }

    private void UpdateIsDragDropEnabled()
    {
        var list = Interaction.GetBehaviors(this);
        if (_leftDockArea != null || _rightDockArea != null)
        {
            if (_dragEventSubscribed) return;
            _dragEventSubscribed = true;
            list.Add((DockAreaDragDropBehavior?)Activator.CreateInstance(DockAreaDragDropBehavior.GetBehaviorType(this))!);
        }
        else
        {
            if (!_dragEventSubscribed) return;
            _dragEventSubscribed = false;

            list.RemoveAll(list.OfType<DockAreaDragDropBehavior>());
        }
    }

    private void OnBehaviorTypeChanged(AvaloniaPropertyChangedEventArgs<Type> e)
    {
        if (_dragEventSubscribed && (_leftDockArea != null || _rightDockArea != null))
        {
            var list = Interaction.GetBehaviors(this);
            list.RemoveAll(list.OfType<DockAreaDragDropBehavior>());
            var type = e.NewValue.GetValueOrDefault() ?? typeof(DockAreaDragDropBehavior);
            var newBehavior = Activator.CreateInstance(type);
            list.Add((DockAreaDragDropBehavior)newBehavior!);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LeftContentProperty ||
            change.Property == RightContentProperty)
        {
            ContentChanged(change);
        }
        else if (change.Property == LeftWidthProportionProperty ||
                 change.Property == RightWidthProportionProperty)
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
        _leftPresenter = e.NameScope.Get<ContentPresenter>("PART_LeftContentPresenter");
        _rightPresenter = e.NameScope.Get<ContentPresenter>("PART_RightContentPresenter");
        _root = e.NameScope.Get<Panel>("PART_Root");
        _thumb = e.NameScope.Get<Thumb>("PART_Thumb");

        var leftVisibilityObservable = _leftPresenter.IsChildVisibleObservable();
        var rightVisibilityObservable = _rightPresenter.IsChildVisibleObservable();

        _thumb.Bind(IsVisibleProperty, leftVisibilityObservable
            .CombineLatest(rightVisibilityObservable, (left, right) => left && right));

        leftVisibilityObservable.CombineLatest(rightVisibilityObservable)
            .Do(_ => UpdateSize(Bounds.Size))
            .Subscribe(t => IsVisible = t.Item1 || t.Item2);

        _thumb.DragDelta += OnThumbDragDelta;
    }

    private void OnThumbDragDelta(object? sender, VectorEventArgs e)
    {
        if (_leftPresenter == null || _rightPresenter == null || _root == null || _thumb == null)
            return;

        var leftWidth = _leftPresenter.Width;
        var bottomHeight = _rightPresenter.Width;
        var delta = e.Vector.X;
        var size = Bounds.Size;

        var newWidth = leftWidth + delta;
        if (newWidth + 5 >= size.Width || newWidth <= 5)
            return;

        var leftWidthProportion = newWidth / size.Width;
        var rightWidthProportion = 1 - leftWidthProportion;
        LeftWidthProportion = Math.Clamp(leftWidthProportion, 0, 1);
        RightWidthProportion = Math.Clamp(rightWidthProportion, 0, 1);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateSize(e.NewSize);
    }

    private void UpdateSize(Size size)
    {
        if (_leftPresenter == null || _rightPresenter == null || _thumb == null)
            return;
        (double leftWidth, double rightWidth) = GetAbsoluteHeight(size);

        if (_leftPresenter.IsChildVisible() && _rightPresenter.IsChildVisible())
        {
            _leftPresenter.Margin = new Thickness(0, 0, 0, 0);
            _leftPresenter.Width = leftWidth;

            _thumb.Margin = new Thickness(leftWidth - ThumbPadding, 0, 0, 0);

            _rightPresenter.Margin = new Thickness(leftWidth + ThumbPadding, 0, 0, 0);
            _rightPresenter.Width = rightWidth - ThumbPadding;
        }
        else
        {
            if (_leftPresenter.IsChildVisible())
            {
                _leftPresenter.Margin = new Thickness(0, 0, 0, 0);
                _leftPresenter.Width = size.Width;
            }
            else if (_rightPresenter.IsChildVisible())
            {
                _rightPresenter.Margin = new Thickness(0, 0, 0, 0);
                _rightPresenter.Width = size.Width;
            }
        }
    }

    private (double, double) GetAbsoluteHeight(Size availableSize)
    {
        var den = LeftWidthProportion + RightWidthProportion;
        return (availableSize.Width * LeftWidthProportion / den,
            availableSize.Width * RightWidthProportion / den);
    }
}