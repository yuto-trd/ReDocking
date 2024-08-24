using System;

using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace ReDocking;

public class ReDockHost : ContentControl
{
    public static readonly RoutedEvent<EdgeBarButtonMoveEventArgs> ButtonMoveEvent =
        RoutedEvent.Register<ReDockHost, EdgeBarButtonMoveEventArgs>(nameof(ButtonMove), RoutingStrategies.Bubble);

    public AvaloniaList<DockArea> DockAreas { get; } = [];

    public event EventHandler<EdgeBarButtonMoveEventArgs> ButtonMove
    {
        add => AddHandler(ButtonMoveEvent, value);
        remove => RemoveHandler(ButtonMoveEvent, value);
    }

    internal void ShowFlyout(EdgeBarButton button)
    {
        var flyout = new EdgeBarButtonMenuFlyout(this);
        if (button.DockLocation?.HasFlag(DockAreaLocation.Left) == true)
        {
            flyout.Placement = PlacementMode.Right;
        }
        else
        {
            flyout.Placement = PlacementMode.Left;
        }

        flyout.ShowAt(button);
    }
}