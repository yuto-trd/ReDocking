using System;

namespace ReDocking;
//
// [Flags]
// public enum DockAreaLocation
// {
//     Left = 1,
//     Top = 1 << 1,
//     Right = 1 << 2,
//     Bottom = 1 << 3,
//
//     TopLeft = Top | Left,
//     BottomLeft = Bottom | Left,
//     TopRight = Top | Right,
//     BottomRight = Bottom | Right,
// }

public enum SideBarButtonLocation
{
    UpperTop,
    UpperBottom,
    LowerTop,
    LowerBottom,
}

public enum SideBarLocation
{
    Left,
    Right
}

public record DockAreaLocation(SideBarButtonLocation ButtonLocation, SideBarLocation LeftRight)
{
    public static readonly DockAreaLocation LeftUpperTop =
        new(SideBarButtonLocation.UpperTop, SideBarLocation.Left);

    public static readonly DockAreaLocation LeftUpperBottom =
        new(SideBarButtonLocation.UpperBottom, SideBarLocation.Left);

    public static readonly DockAreaLocation LeftLowerTop =
        new(SideBarButtonLocation.LowerTop, SideBarLocation.Left);

    public static readonly DockAreaLocation LeftLowerBottom =
        new(SideBarButtonLocation.LowerBottom, SideBarLocation.Left);

    public static readonly DockAreaLocation RightUpperTop =
        new(SideBarButtonLocation.UpperTop, SideBarLocation.Right);

    public static readonly DockAreaLocation RightUpperBottom =
        new(SideBarButtonLocation.UpperBottom, SideBarLocation.Right);

    public static readonly DockAreaLocation RightLowerTop =
        new(SideBarButtonLocation.LowerTop, SideBarLocation.Right);

    public static readonly DockAreaLocation RightLowerBottom =
        new(SideBarButtonLocation.LowerBottom, SideBarLocation.Right);

    public static DockAreaLocation Parse(string s)
    {
        if (string.Equals(s, "LeftUpperTop", StringComparison.OrdinalIgnoreCase))
        {
            return LeftUpperTop;
        }

        if (string.Equals(s, "LeftUpperBottom", StringComparison.OrdinalIgnoreCase))
        {
            return LeftUpperBottom;
        }

        if (string.Equals(s, "LeftLowerTop", StringComparison.OrdinalIgnoreCase))
        {
            return LeftLowerTop;
        }

        if (string.Equals(s, "LeftLowerBottom", StringComparison.OrdinalIgnoreCase))
        {
            return LeftLowerBottom;
        }

        if (string.Equals(s, "RightUpperTop", StringComparison.OrdinalIgnoreCase))
        {
            return RightUpperTop;
        }

        if (string.Equals(s, "RightUpperBottom", StringComparison.OrdinalIgnoreCase))
        {
            return RightUpperBottom;
        }

        if (string.Equals(s, "RightLowerTop", StringComparison.OrdinalIgnoreCase))
        {
            return RightLowerTop;
        }

        if (string.Equals(s, "RightLowerBottom", StringComparison.OrdinalIgnoreCase))
        {
            return RightLowerBottom;
        }
        
        throw new ArgumentException("Invalid DockAreaLocation string", nameof(s));
    }
}