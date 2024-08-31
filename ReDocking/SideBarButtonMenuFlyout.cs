using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ReDocking;

internal class SideBarButtonMenuFlyout : MenuFlyout
{
    private readonly ReDockHost _dockHost;

    public SideBarButtonMenuFlyout(ReDockHost dockHost)
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
            Target is SideBarButton button)
        {
            // Target
            var oldSideBar = button.FindAncestorOfType<SideBar>();
            var newSideBar = area.SideBar;
            if (oldSideBar is null || newSideBar is null) return;
            var oldLocation = button.DockLocation;
            var newLocation = area.Location;
            if (oldLocation is null || oldLocation.Value == newLocation) return;

            var args = new SideBarButtonMoveEventArgs(ReDockHost.ButtonMoveEvent, this)
            {
                Item = button.DataContext,
                Button = button,
                SourceSideBar = oldSideBar,
                SourceLocation = oldLocation.Value,
                DestinationSideBar = newSideBar,
                DestinationLocation = newLocation,
                DestinationIndex = 0
            };
            _dockHost.RaiseEvent(args);
        }
    }
}