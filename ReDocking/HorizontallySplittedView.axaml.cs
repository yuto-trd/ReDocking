using System;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace ReDocking;

public class HorizontallySplittedView : TemplatedControl
{
    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<HorizontallySplittedView, object?>(nameof(LeftContent));

    public static readonly StyledProperty<IDataTemplate?> LeftContentTemplateProperty =
        AvaloniaProperty.Register<HorizontallySplittedView, IDataTemplate?>(nameof(LeftContentTemplate));

    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<HorizontallySplittedView, object?>(nameof(RightContent));

    public static readonly StyledProperty<IDataTemplate?> RightContentTemplateProperty =
        AvaloniaProperty.Register<HorizontallySplittedView, IDataTemplate?>(nameof(RightContentTemplate));

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LeftContentProperty ||
            change.Property == RightContentProperty)
        {
            ContentChanged(change);
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
        var leftPresenter = e.NameScope.Get<ContentPresenter>("PART_LeftContentPresenter");
        var rightPresenter = e.NameScope.Get<ContentPresenter>("PART_RightContentPresenter");
        var splitter = e.NameScope.Get<GridSplitter>("PART_Splitter");

        var leftVisibilityObservable = leftPresenter.IsChildVisibleObservable();
        var rightVisibilityObservable = rightPresenter.IsChildVisibleObservable();

        splitter.Bind(IsVisibleProperty, leftVisibilityObservable
            .CombineLatest(rightVisibilityObservable, (left, right) => left && right)
            .Do(v => rightPresenter.BorderThickness = new Thickness(v ? 1 : 0, 0, 0, 0)));

        leftVisibilityObservable.CombineLatest(rightVisibilityObservable)
            .Subscribe(t => IsVisible = t.Item1 || t.Item2);
    }
}