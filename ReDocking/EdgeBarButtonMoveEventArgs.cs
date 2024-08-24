using Avalonia.Interactivity;

namespace ReDocking;

public class EdgeBarButtonMoveEventArgs(RoutedEvent? routedEvent, object? source) : RoutedEventArgs(routedEvent, source)
{
    public required object? Item { get; init; }

    public required EdgeBarButton Button { get; init; }

    public required EdgeBar SourceEdgeBar { get; init; }

    public required DockAreaLocation SourceLocation { get; init; }

    public required EdgeBar DestinationEdgeBar { get; init; }

    public required DockAreaLocation DestinationLocation { get; init; }

    public required int DestinationIndex { get; init; }
}