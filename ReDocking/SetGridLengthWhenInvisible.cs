using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace ReDocking;

public class SetGridLengthWhenInvisible : Behavior<Control>
{
    public GridLength Value { get; set; }
    
    public static void SetSubscribe(Control control, GridLength value)
    {
        Interaction.GetBehaviors(control).Add(new SetGridLengthWhenInvisible()
        {
            Value = value
        });
    }

    private IDisposable? _disposable;

    protected override void OnAttached()
    {
        base.OnAttached();
        _disposable = AssociatedObject?.GetObservable(Visual.IsVisibleProperty)
            .Subscribe(OnIsVisibleChanged);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        _disposable?.Dispose();
    }

    private void OnIsVisibleChanged(bool value)
    {
        if (AssociatedObject?.IsVisible != false) return;
        var grid = AssociatedObject.FindAncestorOfType<Grid>();
        if (grid == null) return;

        var column = Grid.GetColumn(AssociatedObject);
        if (column < grid.ColumnDefinitions.Count)
        {
            var definition = grid.ColumnDefinitions[column];
            definition.Width = Value;
        }

        var row = Grid.GetRow(AssociatedObject);
        if (row < grid.RowDefinitions.Count)
        {
            var definition = grid.RowDefinitions[row];
            definition.Height = Value;
        }
    }
}