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

    public IconSource IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    internal EdgeBarButtonLocation? Location { get; set; }

    internal EdgeBarLocation? EdgeBarLocation { get; set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var itemsControl = this.FindAncestorOfType<ItemsControl>();
        Location = itemsControl?.Name switch
        {
            "PART_TopTools" => EdgeBarButtonLocation.Top,
            "PART_Tools" => EdgeBarButtonLocation.Middle,
            "PART_BottomTools" => EdgeBarButtonLocation.Bottom,
            _ => null
        };
        var edgeBar = this.FindAncestorOfType<EdgeBar>();
        EdgeBarLocation = edgeBar?.Location;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Location = null;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _canDrag = true;
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
            _canDrag = false;
            var edgeBar = this.FindAncestorOfType<EdgeBar>();
            edgeBar?.SetGridHitTestVisible(false);
            ToolTip.SetServiceEnabled(this, false);
            IsVisible = false;
            if (Parent is ContentPresenter cp)
            {
                cp.IsVisible = false;
            }

            var data = new DataObject();
            data.Set("EdgeBarButton", this);
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            edgeBar?.SetGridHitTestVisible(true);
            ToolTip.SetServiceEnabled(this, true);
            IsVisible = true;
            if (Parent is ContentPresenter cp2)
            {
                cp2.IsVisible = true;
            }
        }
    }
}