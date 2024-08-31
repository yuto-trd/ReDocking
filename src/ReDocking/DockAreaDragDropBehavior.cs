using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace ReDocking;

public class DockAreaDragDropBehavior : Behavior<Control>
{
    private Border? _dragGhost;
    private AdornerLayer? _layer;
    private (DockArea, Control)[]? _areas;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            DragDrop.SetAllowDrop(AssociatedObject, true);
            AssociatedObject.AddHandler(DragDrop.DropEvent, OnDrop);
            AssociatedObject.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
            AssociatedObject.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            AssociatedObject.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            DragDrop.SetAllowDrop(AssociatedObject, false);
            AssociatedObject.RemoveHandler(DragDrop.DropEvent, OnDrop);
            AssociatedObject.RemoveHandler(DragDrop.DragEnterEvent, OnDragEnter);
            AssociatedObject.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
            AssociatedObject.RemoveHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("SideBarButton"))
        {
            DeleteDragGhost();
            if (_areas == null || AssociatedObject == null)
                return;

            if (e.Data.Get("SideBarButton") is not SideBarButton { DockLocation: not null } button)
                return;

            foreach ((DockArea? dockArea, Control? control) in _areas)
            {
                // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (dockArea == null || control == null || dockArea.SideBar == null)
                    continue;
                // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

                if (HoverContentPresenter(control, e))
                {
                    var oldSideBar = button.FindAncestorOfType<SideBar>();
                    if (oldSideBar == null) return;

                    var args = new SideBarButtonMoveEventArgs(ReDockHost.ButtonMoveEvent, AssociatedObject)
                    {
                        Item = button.DataContext,
                        Button = button,
                        SourceSideBar = oldSideBar,
                        SourceLocation = button.DockLocation.Value,
                        DestinationSideBar = dockArea.SideBar,
                        DestinationLocation = dockArea.Location,
                        DestinationIndex = 0
                    };
                    AssociatedObject.RaiseEvent(args);

                    break;
                }
            }
        }
    }

    private void CreateDragGhost()
    {
        _dragGhost = new Border
        {
            Background = AssociatedObject!.FindResource("AccentFillColorDefaultBrush") as IBrush,
            IsHitTestVisible = false,
            Opacity = 0.5,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _layer = AdornerLayer.GetAdornerLayer(AssociatedObject!);
        _layer?.Children.Add(_dragGhost);
    }

    private void DeleteDragGhost()
    {
        if (_layer == null || _dragGhost == null)
            return;
        _layer?.Children.Remove(_dragGhost);
        _dragGhost = null;
        _layer = null;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("SideBarButton"))
        {
            _areas = (AssociatedObject as IDockAreaView)!.GetArea();
            CreateDragGhost();
            OnDragOver(sender, e);
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (_areas == null || _dragGhost == null || _layer == null)
            return;

        if (e.Data.Contains("SideBarButton"))
        {
            bool flag = false;
            foreach ((DockArea? dockArea, Control? control) in _areas)
            {
                // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (dockArea == null || control == null)
                    continue;
                // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

                flag = HoverContentPresenter(control, e);
                if (flag)
                {
                    break;
                }
            }

            _dragGhost.IsVisible = flag;
        }
    }

    private bool HoverContentPresenter(Control presenter, DragEventArgs e)
    {
        if ((presenter as ContentPresenter)?.IsChildVisible() == false)
            return false;
        
        var position = e.GetPosition(presenter);
        if (!presenter.Bounds.WithX(0).WithY(0).Contains(position))
            return false;

        if (_layer != null && _dragGhost != null)
        {
            var ghostPos = _layer.PointToClient(presenter.PointToScreen(default));
            _dragGhost.Margin = new Thickness(ghostPos.X, ghostPos.Y, 0, 0);
            _dragGhost.Width = presenter.Bounds.Width;
            _dragGhost.Height = presenter.Bounds.Height;
        }

        return true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("SideBarButton"))
        {
            DeleteDragGhost();
        }
    }
}