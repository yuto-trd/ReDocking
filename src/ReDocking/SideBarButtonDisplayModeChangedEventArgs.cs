using Avalonia.Interactivity;

namespace ReDocking;

public class SideBarButtonDisplayModeChangedEventArgs(RoutedEvent? routedEvent, object? source)
    : RoutedEventArgs(routedEvent, source)
{
    public DockableDisplayMode DisplayMode { get; init; }
    
    public required object? Item { get; init; }

    public required SideBarButton Button { get; init; }
}