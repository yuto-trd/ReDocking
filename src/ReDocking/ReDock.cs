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

public class ReDock : TemplatedControl, IDockAreaView
{
    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<ReDock, object?>(nameof(LeftContent));

    public static readonly StyledProperty<IDataTemplate?> LeftContentTemplateProperty =
        AvaloniaProperty.Register<ReDock, IDataTemplate?>(nameof(LeftContentTemplate));

    public static readonly StyledProperty<double> LeftWidthProportionProperty =
        AvaloniaProperty.Register<ReDock, double>(nameof(LeftWidthProportion), defaultValue: 1 / 4d);

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<ReDock, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<ReDock, IDataTemplate?>(nameof(ContentTemplate));

    public static readonly StyledProperty<double> WidthProportionProperty =
        AvaloniaProperty.Register<ReDock, double>(nameof(WidthProportion), defaultValue: 1 / 2d);

    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<ReDock, object?>(nameof(RightContent));

    public static readonly StyledProperty<IDataTemplate?> RightContentTemplateProperty =
        AvaloniaProperty.Register<ReDock, IDataTemplate?>(nameof(RightContentTemplate));

    public static readonly StyledProperty<double> RightWidthProportionProperty =
        AvaloniaProperty.Register<ReDock, double>(nameof(RightWidthProportion), defaultValue: 1 / 4d);

    private DockArea? _leftDockArea;
    private DockArea? _rightDockArea;
    private ContentPresenter? _leftPresenter;
    private ContentPresenter? _rightPresenter;
    private ContentPresenter? _presenter;
    private Thumb? _leftThumb;
    private Thumb? _rightThumb;
    private bool _dragEventSubscribed;

    private const double ThumbPadding = 2;

    static ReDock()
    {
        DockAreaDragDropBehavior.BehaviorTypeProperty.Changed.AddClassHandler<ReDock, Type>(
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

    [Content]
    [DependsOn(nameof(ContentTemplate))]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public double WidthProportion
    {
        get => GetValue(WidthProportionProperty);
        set => SetValue(WidthProportionProperty, value);
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

        if (change.Property == ContentProperty ||
            change.Property == LeftContentProperty ||
            change.Property == RightContentProperty)
        {
            ContentChanged(change);
        }
        else if (change.Property == LeftWidthProportionProperty ||
                 change.Property == WidthProportionProperty ||
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
        _presenter = e.NameScope.Get<ContentPresenter>("PART_ContentPresenter");
        _leftThumb = e.NameScope.Get<Thumb>("PART_LeftThumb");
        _rightThumb = e.NameScope.Get<Thumb>("PART_RightThumb");

        var leftVisibilityObservable = _leftPresenter.IsChildVisibleObservable();
        var rightVisibilityObservable = _rightPresenter.IsChildVisibleObservable();
        _leftThumb.Bind(IsVisibleProperty, leftVisibilityObservable);
        _rightThumb.Bind(IsVisibleProperty, rightVisibilityObservable);
        leftVisibilityObservable.CombineLatest(rightVisibilityObservable)
            .Subscribe(_ => UpdateSize(Bounds.Size));

        _leftThumb.DragDelta += OnLeftThumbDragDelta;
        _rightThumb.DragDelta += OnRightThumbDragDelta;
    }

    private void OnLeftThumbDragDelta(object? sender, VectorEventArgs e)
    {
        if (_leftPresenter == null || _rightPresenter == null || _presenter == null)
            return;

        var size = Bounds.Size;
        (double left, double center, double right) = GetAbsoluteWidth(size);
        var delta = e.Vector.X;

        left += delta;

        if (left + 5 >= size.Width || left <= 5)
            return;

        var leftWidthProportion = left / size.Width;
        var rightWidthProportion = right / size.Width;
        var widthProportion = 1 - (leftWidthProportion + rightWidthProportion);
        LeftWidthProportion = Math.Clamp(leftWidthProportion, 0, 1);
        WidthProportion = Math.Clamp(widthProportion, 0, 1);
        RightWidthProportion = Math.Clamp(rightWidthProportion, 0, 1);
    }

    private void OnRightThumbDragDelta(object? sender, VectorEventArgs e)
    {
        if (_leftPresenter == null || _rightPresenter == null || _presenter == null)
            return;

        var size = Bounds.Size;
        (double left, _, double right) = GetAbsoluteWidth(size);
        var delta = e.Vector.X;

        right -= delta;

        if (right + 5 >= size.Width || right <= 5)
            return;

        var leftWidthProportion = left / size.Width;
        var rightWidthProportion = right / size.Width;
        var widthProportion = 1 - (leftWidthProportion + rightWidthProportion);
        LeftWidthProportion = Math.Clamp(leftWidthProportion, 0, 1);
        WidthProportion = Math.Clamp(widthProportion, 0, 1);
        RightWidthProportion = Math.Clamp(rightWidthProportion, 0, 1);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateSize(e.NewSize);
    }

    private void UpdateSize(Size size)
    {
        if (_leftPresenter == null || _presenter == null || _rightPresenter == null ||
            _leftThumb == null || _rightThumb == null)
            return;

        (double leftWidth, double centerWidth, double rightWidth) = GetAbsoluteWidth(size);
        if (_leftPresenter.IsChildVisible() && _rightPresenter.IsChildVisible())
        {
            _leftPresenter.Margin = new Thickness(0, 0, 0, 0);
            _leftPresenter.Width = leftWidth;

            _leftThumb.Margin = new Thickness(leftWidth - ThumbPadding, 0, 0, 0);

            _presenter.Margin = new Thickness(leftWidth + ThumbPadding, 0, 0, 0);
            _presenter.Width = centerWidth - ThumbPadding * 2;

            _rightThumb.Margin = new Thickness(leftWidth + centerWidth - ThumbPadding, 0, 0, 0);

            _rightPresenter.Margin = new Thickness(leftWidth + centerWidth + ThumbPadding, 0, 0, 0);
            _rightPresenter.Width = rightWidth - ThumbPadding;
        }
        else
        {
            if (_leftPresenter.IsChildVisible())
            {
                _leftPresenter.Margin = new Thickness(0, 0, 0, 0);
                _leftPresenter.Width = leftWidth;

                _leftThumb.Margin = new Thickness(leftWidth - ThumbPadding, 0, 0, 0);

                _presenter.Margin = new Thickness(leftWidth + ThumbPadding, 0, 0, 0);
                _presenter.Width = centerWidth - ThumbPadding + rightWidth;
            }
            else if (_rightPresenter.IsChildVisible())
            {
                _presenter.Margin = new Thickness(0, 0, 0, 0);
                _presenter.Width = leftWidth + centerWidth - ThumbPadding;

                _rightThumb.Margin = new Thickness(leftWidth + centerWidth - ThumbPadding, 0, 0, 0);

                _rightPresenter.Margin = new Thickness(leftWidth + centerWidth + ThumbPadding, 0, 0, 0);
                _rightPresenter.Width = rightWidth - ThumbPadding;
            }
            else
            {
                _presenter.Margin = new Thickness(0, 0, 0, 0);
                _presenter.Width = size.Width;
            }
        }
    }

    private (double, double, double) GetAbsoluteWidth(Size availableSize)
    {
        var den = LeftWidthProportion + WidthProportion + RightWidthProportion;
        return (availableSize.Width * LeftWidthProportion / den,
            availableSize.Width * WidthProportion / den,
            availableSize.Width * RightWidthProportion / den);
    }
}