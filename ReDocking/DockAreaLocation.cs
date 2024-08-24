using System;

namespace ReDocking;

[Flags]
public enum DockAreaLocation
{
    Left = 1,
    Top = 1 << 1,
    Right = 1 << 2,
    Bottom = 1 << 3,

    TopLeft = Top | Left,
    BottomLeft = Bottom | Left,
    TopRight = Top | Right,
    BottomRight = Bottom | Right,
}