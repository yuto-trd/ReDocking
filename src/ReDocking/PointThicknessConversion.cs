using Avalonia;

namespace ReDocking;

public static class PointThicknessConversion
{
    public static Thickness ToThickness(this Point point) => new Thickness(point.X, point.Y, 0, 0);
}