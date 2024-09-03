using System;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls.Presenters;

namespace ReDocking;

public static class ObservableHelper
{
    public static IObservable<bool> IsChildVisibleObservable(this ContentPresenter presenter)
    {
        return presenter
            .GetObservable(ContentPresenter.ChildProperty)
            .Select(c => c?.GetObservable(Visual.IsVisibleProperty) ?? Observable.Return(false))
            .Switch()
            .CombineLatest(
                presenter.GetObservable(ContentPresenter.ContentProperty),
                (x, y) => x && y != null)
            .DistinctUntilChanged();
    }

    public static bool IsChildVisible(this ContentPresenter presenter)
    {
        return presenter.Child?.IsVisible == true && presenter.Content != null;
    }
}