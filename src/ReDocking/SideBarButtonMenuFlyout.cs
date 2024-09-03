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
        var list = new List<Control>();

        {
            var moveMenu = new MenuItem();
            moveMenu.Header = "Move to";
            moveMenu.ItemsSource = dockHost.DockAreas;
            moveMenu.DataTemplates.Add(new FuncDataTemplate<DockArea>(_ => true,
                o => new TextBlock
                {
                    [!TextBlock.TextProperty] = o.GetObservable(DockArea.LocalizedNameProperty).ToBinding(),
                }));

            moveMenu.AddHandler(MenuItem.ClickEvent, OnMoveToSubItemClick);
            list.Add(moveMenu);
        }

        if (dockHost.IsFloatingEnabled)
        {
            var displayMenu = new MenuItem();
            displayMenu.Header = "Display mode";
            displayMenu.ItemsSource = new List<Control>
            {
                new MenuItem { Header = "Docked", Tag = DockableDisplayMode.Docked },
                new MenuItem { Header = "Floating", Tag = DockableDisplayMode.Floating },
            };
            displayMenu.AddHandler(MenuItem.ClickEvent, OnDisplayModeClick);
            list.Add(displayMenu);
        }

        ItemsSource = list;
    }

    private void OnDisplayModeClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is MenuItem { Tag: DockableDisplayMode mode } &&
            Target is SideBarButton button)
        {
            var args = new SideBarButtonDisplayModeChangedEventArgs(ReDockHost.ButtonDisplayModeChangedEvent, this)
            {
                DisplayMode = mode,
                Item = button.DataContext,
                Button = button
            };
            _dockHost.RaiseEvent(args);
        }
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
            if (oldLocation is null || oldLocation == newLocation) return;

            var args = new SideBarButtonMoveEventArgs(ReDockHost.ButtonMoveEvent, this)
            {
                Item = button.DataContext,
                Button = button,
                SourceSideBar = oldSideBar,
                SourceLocation = oldLocation,
                DestinationSideBar = newSideBar,
                DestinationLocation = newLocation,
                DestinationIndex = 0
            };
            _dockHost.RaiseEvent(args);
        }
    }
}