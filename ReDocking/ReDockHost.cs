using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;

namespace ReDocking;

public enum DockAreaLocation
{
    Left,
    TopLeft,
    BottomLeft,
    Right,
    TopRight,
    BottomRight,
}

public class DockArea : AvaloniaObject
{
    public static readonly StyledProperty<DockAreaLocation> LocationProperty =
        AvaloniaProperty.Register<DockArea, DockAreaLocation>(nameof(Location));

    public static readonly StyledProperty<Control?> ViewProperty = 
        AvaloniaProperty.Register<DockArea, Control?>(nameof(View));

    public static readonly StyledProperty<string?> TargetProperty =
        AvaloniaProperty.Register<DockArea, string?>("Target");

    public static readonly StyledProperty<string?> LocalizedNameProperty =
        AvaloniaProperty.Register<DockArea, string?>(nameof(LocalizedName));

    public string? LocalizedName
    {
        get => GetValue(LocalizedNameProperty);
        set => SetValue(LocalizedNameProperty, value);
    }

    public string? Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    [ResolveByName]
    public Control? View
    {
        get => GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    public DockAreaLocation Location
    {
        get => GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }
}

public class ReDockHost : ContentControl
{
    public AvaloniaList<DockArea> DockAreas { get; } = [];
}