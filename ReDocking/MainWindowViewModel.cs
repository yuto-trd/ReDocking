using System;
using System.Linq;
using Avalonia.Reactive;
using Avalonia.Reactive.Operators;
using ObservableCollections;
using R3;

namespace ReDocking;

public class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel()
    {
        void ConfigureToolsList(ObservableList<ToolWindowViewModel> list,
            BindableReactiveProperty<ToolWindowViewModel?> selected)
        {
            list.ObserveAdd()
                .Select(x => x.Value.IsSelected.Select(y => (x.Value, y)))
                .Switch()
                .Subscribe(z => selected.Value = z.y ? z.Item1 : null);

            list.ObserveRemove()
                .Subscribe(x => x.Value.Dispose());

            selected.Subscribe(x =>
                list.ToObservable()
                    .Where(y => y != x)
                    .Subscribe(y => y.IsSelected.Value = false));
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

    public ObservableList<ToolWindowViewModel> LeftTopTools { get; } = [];

    public BindableReactiveProperty<ToolWindowViewModel?> SelectedLeftTopTool { get; } = new();

    public ObservableList<ToolWindowViewModel> LeftTools { get; } = [];

    public BindableReactiveProperty<ToolWindowViewModel?> SelectedLeftTool { get; } = new();

    public ObservableList<ToolWindowViewModel> LeftBottomTools { get; } = [];

    public BindableReactiveProperty<ToolWindowViewModel?> SelectedLeftBottomTool { get; } = new();

    public ObservableList<ToolWindowViewModel> RightTopTools { get; } = [];

    public BindableReactiveProperty<ToolWindowViewModel?> SelectedRightTopTool { get; } = new();

    public ObservableList<ToolWindowViewModel> RightTools { get; } = [];

    public BindableReactiveProperty<ToolWindowViewModel?> SelectedRightTool { get; } = new();

    public ObservableList<ToolWindowViewModel> RightBottomTools { get; } = [];

    public BindableReactiveProperty<ToolWindowViewModel?> SelectedRightBottomTool { get; } = new();

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
        Icon.Value = icon;
    }

    public BindableReactiveProperty<string> Icon { get; } = new();

    public BindableReactiveProperty<string> Name { get; } = new();

    public BindableReactiveProperty<object> Content { get; } = new();

    public BindableReactiveProperty<bool> IsSelected { get; } = new(false);

    public void Dispose()
    {
        Icon.Dispose();
        Name.Dispose();
        IsSelected.Dispose();
    }
}