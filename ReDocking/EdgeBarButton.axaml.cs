using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

using FluentAvalonia.UI.Controls;

namespace ReDocking;

public class EdgeBarButton : ToggleButton
{
    public static readonly StyledProperty<IconSource> IconSourceProperty = 
        AvaloniaProperty.Register<EdgeBarButton, IconSource>(nameof(IconSource));

    public IconSource IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }
}