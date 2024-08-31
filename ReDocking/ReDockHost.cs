using System;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ReDocking;

public class ReDockHost : ContentControl
{
    public static readonly RoutedEvent<SideBarButtonMoveEventArgs> ButtonMoveEvent =
        RoutedEvent.Register<ReDockHost, SideBarButtonMoveEventArgs>(nameof(ButtonMove), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<SideBarButtonDisplayModeChangedEventArgs> ButtonDisplayModeChangedEvent =
        RoutedEvent.Register<ReDockHost, SideBarButtonDisplayModeChangedEventArgs>(nameof(ButtonDisplayModeChanged), RoutingStrategies.Bubble);

    public static readonly StyledProperty<bool> IsFloatingEnabledProperty =
        AvaloniaProperty.Register<ReDockHost, bool>(nameof(IsFloatingEnabled));

    public bool IsFloatingEnabled
    {
        get => GetValue(IsFloatingEnabledProperty);
        set => SetValue(IsFloatingEnabledProperty, value);
    }
    
    public AvaloniaList<DockArea> DockAreas { get; } = [];

    public event EventHandler<SideBarButtonMoveEventArgs> ButtonMove
    {
        add => AddHandler(ButtonMoveEvent, value);
        remove => RemoveHandler(ButtonMoveEvent, value);
    }

    public event EventHandler<SideBarButtonDisplayModeChangedEventArgs> ButtonDisplayModeChanged
    {
        add => AddHandler(ButtonDisplayModeChangedEvent, value);
        remove => RemoveHandler(ButtonDisplayModeChangedEvent, value);
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