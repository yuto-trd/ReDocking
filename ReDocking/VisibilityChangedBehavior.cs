using System;
using System.Linq;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace ReDocking;

public class VisibilityChangedBehavior : Behavior<ContentPresenter>
{
    public GridLength Value { get; set; }

    public int? Row { get; set; }

    public int? Column { get; set; }

    public static void SetGridLength(ContentPresenter control, GridLength value)
    {
        GetBehavior(control).Value = value;
    }

    public static void SetRow(ContentPresenter control, int value)
    {
        GetBehavior(control).Row = value;
    }

    public static void SetColumn(ContentPresenter control, int value)
    {
        GetBehavior(control).Column = value;
    }

    private static VisibilityChangedBehavior GetBehavior(ContentPresenter control)
    {
        var list = Interaction.GetBehaviors(control);
        var behavior = list.OfType<VisibilityChangedBehavior>().FirstOrDefault();
        if (behavior == null)
        {
            behavior = new VisibilityChangedBehavior();
            list.Add(behavior);
        }

        return behavior;
    }

    private IDisposable? _disposable;

    protected override void OnAttached()
    {
        base.OnAttached();
        _disposable = AssociatedObject?.GetObservable(ContentPresenter.ChildProperty)
            .Select(c => c?.GetObservable(Visual.IsVisibleProperty) ?? Observable.Return(false))
            .Switch()
            .Subscribe(OnIsVisibleChanged);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        _disposable?.Dispose();
    }

    private void OnIsVisibleChanged(bool value)
    {
        if (value || AssociatedObject == null) return;
        var grid = AssociatedObject.FindAncestorOfType<Grid>();
        if (grid == null) return;

        var column = Column ?? Grid.GetColumn(AssociatedObject);
        if (column < grid.ColumnDefinitions.Count)
        {
            var definition = grid.ColumnDefinitions[column];
            definition.Width = Value;
        }

        var row = Row ?? Grid.GetRow(AssociatedObject);
        if (row < grid.RowDefinitions.Count)
        {
            var definition = grid.RowDefinitions[row];
            definition.Height = Value;
        }
    }
}