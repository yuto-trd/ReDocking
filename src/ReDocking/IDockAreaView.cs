using Avalonia.Controls;

namespace ReDocking;

public interface IDockAreaView
{
    (DockArea, Control)[] GetArea();
    
    void OnAttachedToDockArea(DockArea dockArea)
    {
    }

    void OnDetachedFromDockArea(DockArea dockArea)
    {
    }
}