using Avalonia;
using Avalonia.Controls;

namespace ReDocking;

public class DockArea : AvaloniaObject
{
    public static readonly StyledProperty<DockAreaLocation> LocationProperty =
        AvaloniaProperty.Register<DockArea, DockAreaLocation>(nameof(Location));

    public static readonly StyledProperty<IDockAreaView?> ViewProperty =
        AvaloniaProperty.Register<DockArea, IDockAreaView?>(nameof(View));

    public static readonly StyledProperty<SideBar?> SideBarProperty =
        AvaloniaProperty.Register<DockArea, SideBar?>(nameof(SideBar));

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
    public IDockAreaView? View
    {
        get => GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    [ResolveByName]
    public SideBar? SideBar
    {
        get => GetValue(SideBarProperty);
        set => SetValue(SideBarProperty, value);
    }

    public DockAreaLocation Location
    {
        get => GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewProperty)
        {
            if (change.OldValue is IDockAreaView oldView)
            {
                oldView.OnDetachedFromDockArea(this);
            }

            if (change.NewValue is IDockAreaView newView)
            {
                newView.OnAttachedToDockArea(this);
            }
        }
    }
}