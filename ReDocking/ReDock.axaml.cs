using System;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Reactive;
using Avalonia.Reactive.Operators;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace ReDocking;

public class ReDock : TemplatedControl
{
    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<ReDock, object?>(nameof(LeftContent));

    public static readonly StyledProperty<IDataTemplate?> LeftContentTemplateProperty =
        AvaloniaProperty.Register<ReDock, IDataTemplate?>(nameof(LeftContentTemplate));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<ReDock, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<ReDock, IDataTemplate?>(nameof(ContentTemplate));

    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<ReDock, object?>(nameof(RightContent));

    public static readonly StyledProperty<IDataTemplate?> RightContentTemplateProperty =
        AvaloniaProperty.Register<ReDock, IDataTemplate?>(nameof(RightContentTemplate));

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

        if (change.Property == ContentProperty ||
            change.Property == LeftContentProperty ||
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
        var leftSplitter = e.NameScope.Get<GridSplitter>("PART_LeftSplitter");
        var rightPresenter = e.NameScope.Get<ContentPresenter>("PART_RightContentPresenter");
        var rightSplitter = e.NameScope.Get<GridSplitter>("PART_RightSplitter");

        var leftVisibilityObservable = leftPresenter.IsChildVisibleObservable();
        var rightVisibilityObservable = rightPresenter.IsChildVisibleObservable();
        leftSplitter.Bind(IsVisibleProperty, leftVisibilityObservable
            .Do(v => leftPresenter.BorderThickness = new Thickness(0, 0, v ? 1 : 0, 0)));

        rightSplitter.Bind(IsVisibleProperty, rightVisibilityObservable
            .Do(v => rightPresenter.BorderThickness = new Thickness(v ? 1 : 0, 0, 0, 0)));
    }
}