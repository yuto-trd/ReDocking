using System;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace ReDocking;

public class HorizontallySplittedView : TemplatedControl
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

    private ContentPresenter? _leftPresenter;
    private ContentPresenter? _rightPresenter;
    private Thumb? _thumb;
    private Panel? _root;

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

        if (leftWidth + delta < 0)
            return;

        var newWidth = leftWidth + delta;
        var size = Bounds.Size;
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

            _thumb.Margin = new Thickness(leftWidth - 2, 0, 0, 0);

            _rightPresenter.Margin = new Thickness(leftWidth + 2, 0, 0, 0);
            _rightPresenter.Width = rightWidth - 2;
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