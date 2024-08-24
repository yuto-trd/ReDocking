using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ReDocking;

internal class EdgeBarButtonMenuFlyout : MenuFlyout
{
    private readonly ReDockHost _dockHost;

    public EdgeBarButtonMenuFlyout(ReDockHost dockHost)
    {
        _dockHost = dockHost;
        var moveMenu = new MenuItem();
        moveMenu.Header = "Move to";
        moveMenu.ItemsSource = dockHost.DockAreas;
        moveMenu.DataTemplates.Add(new FuncDataTemplate<DockArea>(_ => true,
            o => new TextBlock
            {
                [!TextBlock.TextProperty] = o.GetObservable(DockArea.LocalizedNameProperty).ToBinding(),
            }));

        moveMenu.AddHandler(MenuItem.ClickEvent, OnMoveToSubItemClick);
        ItemsSource = new List<Control> { moveMenu };
    }

    private void OnMoveToSubItemClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is MenuItem { DataContext: DockArea area } &&
            Target is EdgeBarButton button)
        {
            // Target
            var oldEdgeBar = button.FindAncestorOfType<EdgeBar>();
            var newEdgeBar = area.EdgeBar;
            if (oldEdgeBar is null || newEdgeBar is null) return;
            var oldLocation = button.DockLocation;
            var newLocation = area.Location;
            if (oldLocation is null || oldLocation.Value == newLocation) return;

            var args = new EdgeBarButtonMoveEventArgs(ReDockHost.ButtonMoveEvent, this)
            {
                Item = button.DataContext,
                Button = button,
                SourceEdgeBar = oldEdgeBar,
                SourceLocation = oldLocation.Value,
                DestinationEdgeBar = newEdgeBar,
                DestinationLocation = newLocation,
                DestinationIndex = 0
            };
            _dockHost.RaiseEvent(args);
        }
    }
}