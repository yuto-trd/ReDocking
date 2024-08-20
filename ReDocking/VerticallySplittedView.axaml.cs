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

public class VerticallySplittedView : TemplatedControl
{
    public static readonly StyledProperty<object?> TopContentProperty =
        AvaloniaProperty.Register<VerticallySplittedView, object?>(nameof(TopContent));

    public static readonly StyledProperty<IDataTemplate?> TopContentTemplateProperty =
        AvaloniaProperty.Register<VerticallySplittedView, IDataTemplate?>(nameof(TopContentTemplate));

    public static readonly StyledProperty<object?> BottomContentProperty =
        AvaloniaProperty.Register<VerticallySplittedView, object?>(nameof(BottomContent));

    public static readonly StyledProperty<IDataTemplate?> BottomContentTemplateProperty =
        AvaloniaProperty.Register<VerticallySplittedView, IDataTemplate?>(nameof(BottomContentTemplate));

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TopContentProperty ||
            change.Property == BottomContentProperty)
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
        var topPresenter = e.NameScope.Get<ContentPresenter>("PART_TopContentPresenter");
        var bottomPresenter = e.NameScope.Get<ContentPresenter>("PART_BottomContentPresenter");
        var splitter = e.NameScope.Get<GridSplitter>("PART_Splitter");

        var topVisibilityObservable = topPresenter.IsChildVisibleObservable();
        var bottomVisibilityObservable = bottomPresenter.IsChildVisibleObservable();

        splitter.Bind(IsVisibleProperty, topVisibilityObservable
            .CombineLatest(bottomVisibilityObservable, (left, right) => left && right)
            .Do(v => bottomPresenter.BorderThickness = new Thickness(0, v ? 1 : 0, 0, 0)));

        topVisibilityObservable.CombineLatest(bottomVisibilityObservable)
            .Subscribe(t => IsVisible = t.Item1 || t.Item2);
    }
}