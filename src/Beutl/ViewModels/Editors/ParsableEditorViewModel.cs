﻿using Reactive.Bindings;

namespace Beutl.ViewModels.Editors;

public interface IParsableEditorViewModel
{
    ReadOnlyReactivePropertySlim<bool> CanEdit { get; }

    ReadOnlyReactiveProperty<string> Value { get; }

    string Header { get; }
}

public sealed class ParsableEditorViewModel<T> : BaseEditorViewModel<T>, IParsableEditorViewModel
    where T : IParsable<T>
{
    public ParsableEditorViewModel(IPropertyAdapter<T> property)
        : base(property)
    {
        Value = property.GetObservable()
            .Select(x => x?.ToString() ?? "")
            .ToReadOnlyReactiveProperty()
            .DisposeWith(Disposables)!;
    }

    public ReadOnlyReactiveProperty<string> Value { get; }
}
