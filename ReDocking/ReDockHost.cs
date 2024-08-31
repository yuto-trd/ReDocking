using System;

using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace ReDocking;

public class ReDockHost : ContentControl
{
    public static readonly RoutedEvent<SideBarButtonMoveEventArgs> ButtonMoveEvent =
        RoutedEvent.Register<ReDockHost, SideBarButtonMoveEventArgs>(nameof(ButtonMove), RoutingStrategies.Bubble);

    public AvaloniaList<DockArea> DockAreas { get; } = [];

    public event EventHandler<SideBarButtonMoveEventArgs> ButtonMove
    {
        add => AddHandler(ButtonMoveEvent, value);
        remove => RemoveHandler(ButtonMoveEvent, value);
    }

    internal void ShowFlyout(SideBarButton button)
    {
        var flyout = new SideBarButtonMenuFlyout(this);
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