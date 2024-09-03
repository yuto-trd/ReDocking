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

using Action = System.Action;

namespace ReDocking;

public class DockAreaDragDropBehavior : Behavior<Control>
{
    // HorizontallySplittedView, VerticallySplittedView, ReDockで使われるBehaviorの型
    public static readonly AttachedProperty<Type> BehaviorTypeProperty =
        AvaloniaProperty.RegisterAttached<DockAreaDragDropBehavior, Control, Type>(
            "BehaviorType", defaultValue: typeof(DockAreaDragDropBehavior),
            validate: t => t.IsAssignableTo(typeof(DockAreaDragDropBehavior)));

    private bool _allHidden;
    private bool _anyHidden;
    
    protected Border? DragGhost { get; private set; }

    protected AdornerLayer? Layer { get; private set; }
    
    protected (DockArea, Control)[]? Areas { get; private set; }

    public static void SetBehaviorType(Control obj, Type value)
    {
        obj.SetValue(BehaviorTypeProperty, value);
    }

    public static Type GetBehaviorType(Control obj)
    {
        return obj.GetValue(BehaviorTypeProperty);
    }

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
            if (Areas == null || AssociatedObject == null)
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
                foreach ((DockArea? dockArea, Control? control) in Areas)
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
        DragGhost = new Border
        {
            Background = AssociatedObject!.FindResource("ReDockingGhostBackground") as IBrush,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Layer = AdornerLayer.GetAdornerLayer(AssociatedObject!);
        Layer?.Children.Add(DragGhost);
    }

    private void DeleteDragGhost()
    {
        if (Layer == null || DragGhost == null)
            return;
        Layer?.Children.Remove(DragGhost);
        DragGhost = null;
        Layer = null;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("SideBarButton"))
        {
            Areas = (AssociatedObject as IDockAreaView)!.GetArea();
            _allHidden = Areas.All(i => (i.Item2 as ContentPresenter)?.IsChildVisible() == false);
            _anyHidden = Areas.Any(i => (i.Item2 as ContentPresenter)?.IsChildVisible() == false);

            CreateDragGhost();
            OnDragOver(sender, e);
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (Areas == null || DragGhost == null || Layer == null)
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
                foreach ((DockArea? dockArea, Control? control) in Areas)
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

            DragGhost.IsVisible = flag;
        }
    }

    // いずれかのpresenterが表示されていない場合、2:1で分けて、1の方にポインターが移動したら非表示されている方に移動するようにする
    protected virtual bool HoverSplittedView(DragEventArgs e, out Action postAction, out DockArea? dockArea)
    {
        postAction = () => { };
        dockArea = null;
        if (Areas == null)
            return false;

        var first = Areas[0];
        var second = Areas[1];
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
            if (DragGhost != null && Layer != null)
            {
                DragGhost.Margin = (AssociatedObject.TranslatePoint(default, Layer) ?? default).ToThickness();
                DragGhost.Width = ghostBounds.Width;
                DragGhost.Height = ghostBounds.Height;
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
            if (DragGhost != null && Layer != null)
            {
                var ghostPos = horizontal
                    ? new Point(ghostBounds.Width, 0)
                    : new Point(0, ghostBounds.Height);
                DragGhost.Margin = (AssociatedObject.TranslatePoint(ghostPos, Layer) ?? default).ToThickness();
                DragGhost.Width = ghostBounds.Width;
                DragGhost.Height = ghostBounds.Height;
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
            if (DragGhost != null && Layer != null)
            {
                DragGhost.Margin = (AssociatedObject.TranslatePoint(default, Layer) ?? default).ToThickness();
                DragGhost.Width = bounds.Width;
                DragGhost.Height = bounds.Height;
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

        if (Layer != null && DragGhost != null)
        {
            var ghostPos = Layer.PointToClient(presenter.PointToScreen(default));
            DragGhost.Margin = new Thickness(ghostPos.X, ghostPos.Y, 0, 0);
            DragGhost.Width = presenter.Bounds.Width;
            DragGhost.Height = presenter.Bounds.Height;
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