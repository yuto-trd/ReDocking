using Avalonia.Interactivity;

namespace ReDocking;

public class SideBarButtonMoveEventArgs(RoutedEvent? routedEvent, object? source) : RoutedEventArgs(routedEvent, source)
{
    public required object? Item { get; init; }

    public required SideBarButton Button { get; init; }

    public required SideBar SourceSideBar { get; init; }

    public required DockAreaLocation SourceLocation { get; init; }

    public required SideBar DestinationSideBar { get; init; }

    public required DockAreaLocation DestinationLocation { get; init; }

    public required int DestinationIndex { get; init; }
}