using Avalonia;
using Avalonia.Controls;

namespace ReDocking;

public class DockArea : AvaloniaObject
{
    public static readonly StyledProperty<DockAreaLocation> LocationProperty =
        AvaloniaProperty.Register<DockArea, DockAreaLocation>(nameof(Location));

    public static readonly StyledProperty<Control?> ViewProperty =
        AvaloniaProperty.Register<DockArea, Control?>(nameof(View));

    public static readonly StyledProperty<EdgeBar?> EdgeBarProperty =
        AvaloniaProperty.Register<DockArea, EdgeBar?>(nameof(EdgeBar));

    public static readonly StyledProperty<string?> TargetProperty =
        AvaloniaProperty.Register<DockArea, string?>(nameof(Target));

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

    [ResolveByName]
    public EdgeBar? EdgeBar
    {
        get => GetValue(EdgeBarProperty);
        set => SetValue(EdgeBarProperty, value);
    }

    public DockAreaLocation Location
    {
        get => GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }
}