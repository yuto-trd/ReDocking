using System;

using Reactive.Bindings;

namespace ReDocking.ViewModels;

public class ToolWindowViewModel : IDisposable
{
    public ToolWindowViewModel(string name, string icon, object? content = null)
    {
        Name.Value = name;
        Content.Value = content ?? name;
        Icon.Value = icon;
    }

    public ReactiveProperty<string> Icon { get; } = new();

    public ReactiveProperty<string> Name { get; } = new();

    public ReactiveProperty<object> Content { get; } = new();

    public ReactiveProperty<bool> IsSelected { get; } = new(false);

    public ReactiveProperty<DockableDisplayMode> DisplayMode { get; } = new(DockableDisplayMode.Docked);

    public void Dispose()
    {
        Icon.Dispose();
        Name.Dispose();
        IsSelected.Dispose();
    }
}