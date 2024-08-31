using System;
using System.Linq;
using System.Reactive.Linq;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace ReDocking;

public class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel()
    {
        void ConfigureToolsList(ReactiveCollection<ToolWindowViewModel> list,
            ReactiveProperty<ToolWindowViewModel?> selected)
        {
            selected.Subscribe(x =>
                list.ToObservable()
                    .Where(y => y != x && y.DisplayMode.Value == DockableDisplayMode.Docked)
                    .Subscribe(y => y.IsSelected.Value = false));

            list.ObserveAddChanged()
                .Select(x => x.IsSelected.Select(y => (x, y)))
                .Subscribe(z =>
                {
                    z.Subscribe(w =>
                    {
                        if (w is { y: true, x.DisplayMode.Value: DockableDisplayMode.Docked })
                        {
                            selected.Value = w.x;
                        }
                        else
                        {
                            selected.Value = list.FirstOrDefault(xx =>
                                xx.IsSelected.Value && xx.DisplayMode.Value == DockableDisplayMode.Docked);
                        }
                    });
                });

            list.ObserveRemoveChanged()
                .Subscribe(x => x.Dispose());
        }

        ConfigureToolsList(LeftTopTools, SelectedLeftTopTool);
        ConfigureToolsList(LeftTools, SelectedLeftTool);
        ConfigureToolsList(LeftBottomTools, SelectedLeftBottomTool);
        ConfigureToolsList(RightTopTools, SelectedRightTopTool);
        ConfigureToolsList(RightTools, SelectedRightTool);
        ConfigureToolsList(RightBottomTools, SelectedRightBottomTool);

        LeftTopTools.Add(new ToolWindowViewModel("Library", "\ue8f1"));
        LeftTools.Add(new ToolWindowViewModel("Explorer", "\uec50"));
        LeftBottomTools.Add(new ToolWindowViewModel("Timeline", "\ueca5"));
        RightTopTools.Add(new ToolWindowViewModel("Notifications", "\uea8f"));
        RightTopTools.Add(new ToolWindowViewModel("Notifications2", "\uea8f"));
        RightTools.Add(new ToolWindowViewModel("Properties", "\ue15e"));
        RightTools.Add(new ToolWindowViewModel("Properties2", "\ue15e"));
        RightBottomTools.Add(new ToolWindowViewModel("Path Editor", "\uedfb"));
        RightBottomTools.Add(new ToolWindowViewModel("Path Editor 2", "\uedfb"));
    }

    public ReactiveCollection<ToolWindowViewModel> LeftTopTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedLeftTopTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> LeftTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedLeftTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> LeftBottomTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedLeftBottomTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> RightTopTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedRightTopTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> RightTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedRightTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> RightBottomTools { get; } = [];

    public ReactiveProperty<ToolWindowViewModel?> SelectedRightBottomTool { get; } = new();

    public ReactiveCollection<ToolWindowViewModel> FloatingWindows { get; } = [];

    public void Dispose()
    {
        SelectedLeftTopTool.Dispose();
        SelectedLeftTool.Dispose();
        SelectedLeftBottomTool.Dispose();
        SelectedRightTopTool.Dispose();
        SelectedRightTool.Dispose();
        SelectedRightBottomTool.Dispose();
    }
}

public class ToolWindowViewModel : IDisposable
{
    public ToolWindowViewModel(string name, string icon)
    {
        Name.Value = name;
        Content.Value = name;
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