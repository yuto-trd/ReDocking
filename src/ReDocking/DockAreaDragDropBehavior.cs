using System;
using System.Linq;

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
    private bool _allHidden;
    private bool _anyHidden;

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

            DockArea? detectedArea = null;
            bool notReDock = AssociatedObject is HorizontallySplittedView or VerticallySplittedView;
            if (_anyHidden && notReDock)
            {
                HoverSplittedView(e, out var postAction, out detectedArea);
                postAction();
            }
            else
            {
                foreach ((DockArea? dockArea, Control? control) in _areas)
                {
                    // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    if (dockArea == null || control == null)
                        continue;
                    // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

                    if (HoverContentPresenter(control, e))
                    {
                        detectedArea = dockArea;
                        break;
                    }
                }
            }

            if (detectedArea != null && detectedArea.SideBar != null)
            {
                var oldSideBar = button.FindAncestorOfType<SideBar>();
                if (oldSideBar == null) return;

                var args = new SideBarButtonMoveEventArgs(ReDockHost.ButtonMoveEvent, AssociatedObject)
                {
                    Item = button.DataContext,
                    Button = button,
                    SourceSideBar = oldSideBar,
                    SourceLocation = button.DockLocation,
                    DestinationSideBar = detectedArea.SideBar,
                    DestinationLocation = detectedArea.Location,
                    DestinationIndex = 0
                };
                AssociatedObject.RaiseEvent(args);
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
            _allHidden = _areas.All(i => (i.Item2 as ContentPresenter)?.IsChildVisible() == false);
            _anyHidden = _areas.Any(i => (i.Item2 as ContentPresenter)?.IsChildVisible() == false);

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
            bool notReDock = AssociatedObject is HorizontallySplittedView or VerticallySplittedView;

            if (_allHidden)
            {
                flag = false;
            }
            else if (_anyHidden && notReDock)
            {
                flag = HoverSplittedView(e, out _, out _);
            }
            else
            {
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
            }

            _dragGhost.IsVisible = flag;
        }
    }

    // いずれかのpresenterが表示されていない場合、2:1で分けて、1の方にポインターが移動したら非表示されている方に移動するようにする
    private bool HoverSplittedView(DragEventArgs e, out Action postAction, out DockArea? dockArea)
    {
        postAction = () => { };
        dockArea = null;
        if (_areas == null)
            return false;

        var first = _areas[0];
        var second = _areas[1];
        var horizontal = AssociatedObject is HorizontallySplittedView;
        var size = horizontal
            ? AssociatedObject!.Bounds.Width
            : AssociatedObject!.Bounds.Height;
        var bounds = new Rect(AssociatedObject.Bounds.Size);
        var firstBounds = horizontal
            ? bounds.WithWidth(size / 3)
            : bounds.WithHeight(size / 3);
        var secondBounds = horizontal
            ? firstBounds.WithX(size - firstBounds.Width)
            : firstBounds.WithY(size - firstBounds.Height);
        var ghostBounds = horizontal
            ? bounds.WithWidth(size / 2)
            : bounds.WithHeight(size / 2);

        var position = e.GetPosition(AssociatedObject);
        var firstVisible = (first.Item2 as ContentPresenter)?.IsChildVisible() == true;
        var secondVisible = (second.Item2 as ContentPresenter)?.IsChildVisible() == true;

        if (!firstVisible && firstBounds.Contains(position))
        {
            if (_dragGhost != null && _layer != null)
            {
                _dragGhost.Margin = (AssociatedObject.TranslatePoint(default, _layer) ?? default).ToThickness();
                _dragGhost.Width = ghostBounds.Width;
                _dragGhost.Height = ghostBounds.Height;
            }

            postAction = () =>
            {
                switch (AssociatedObject)
                {
                    case HorizontallySplittedView hsplt:
                        hsplt.LeftWidthProportion = hsplt.RightWidthProportion = 1;
                        break;
                    case VerticallySplittedView vsplt:
                        vsplt.TopHeightProportion = vsplt.BottomHeightProportion = 1;
                        break;
                }
            };
            dockArea = first.Item1;
            return true;
        }

        if (!secondVisible && secondBounds.Contains(position))
        {
            if (_dragGhost != null && _layer != null)
            {
                var ghostPos = horizontal
                    ? new Point(ghostBounds.Width, 0)
                    : new Point(0, ghostBounds.Height);
                _dragGhost.Margin = (AssociatedObject.TranslatePoint(ghostPos, _layer) ?? default).ToThickness();
                _dragGhost.Width = ghostBounds.Width;
                _dragGhost.Height = ghostBounds.Height;
            }

            postAction = () =>
            {
                switch (AssociatedObject)
                {
                    case HorizontallySplittedView hsplt:
                        hsplt.LeftWidthProportion = hsplt.RightWidthProportion = 1;
                        break;
                    case VerticallySplittedView vsplt:
                        vsplt.TopHeightProportion = vsplt.BottomHeightProportion = 1;
                        break;
                }
            };
            dockArea = second.Item1;
            return true;
        }

        if (bounds.Contains(position))
        {
            if (_dragGhost != null && _layer != null)
            {
                _dragGhost.Margin = (AssociatedObject.TranslatePoint(default, _layer) ?? default).ToThickness();
                _dragGhost.Width = bounds.Width;
                _dragGhost.Height = bounds.Height;
            }

            dockArea = firstVisible ? first.Item1 : second.Item1;
            return true;
        }

        return false;
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