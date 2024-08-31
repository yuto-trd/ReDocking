using System;
using System.Collections;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using FluentAvalonia.UI.Controls;

namespace ReDocking;

public class EdgeBarButton : ToggleButton
{
    public static readonly StyledProperty<IconSource> IconSourceProperty =
        AvaloniaProperty.Register<EdgeBarButton, IconSource>(nameof(IconSource));

    private bool _canDrag;
    private Point _startPoint;

    public IconSource IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    internal DockAreaLocation? DockLocation { get; set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var itemsControl = this.FindAncestorOfType<ItemsControl>();
        var l = itemsControl?.Name switch
        {
            "PART_TopTools" => DockAreaLocation.Top,
            "PART_BottomTools" => DockAreaLocation.Bottom,
            _ => default
        };
        var edgeBar = this.FindAncestorOfType<EdgeBar>();
        if (edgeBar != null)
        {
            DockLocation = edgeBar.Location | l;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DockLocation = null;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            this.FindAncestorOfType<ReDockHost>()?.ShowFlyout(this);
        }
        else
        {
            _canDrag = true;
            _startPoint = e.GetPosition(this);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _canDrag = false;
    }

    protected override async void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_canDrag)
        {
            var point = e.GetPosition(this);
            var threshold = Bounds.Height / 4;

            if (!(Math.Abs(point.X - _startPoint.X) > threshold) && !(Math.Abs(point.Y - _startPoint.Y) > threshold))
                return;

            _canDrag = false;
            var edgeBar = this.FindAncestorOfType<EdgeBar>();
            edgeBar?.SetGridHitTestVisible(false);
            IsVisible = false;
            if (Parent is ContentPresenter cp)
            {
                cp.IsVisible = false;
            }

            var data = new DataObject();
            data.Set("EdgeBarButton", this);
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            edgeBar?.SetGridHitTestVisible(true);
            IsVisible = true;
            if (Parent is ContentPresenter cp2)
            {
                cp2.IsVisible = true;
            }
        }
    }
}