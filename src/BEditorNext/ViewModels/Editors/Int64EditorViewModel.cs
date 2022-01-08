﻿using BEditorNext.ProjectSystem;
using BEditorNext.Services.Editors;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace BEditorNext.ViewModels.Editors;

public sealed class Int64EditorViewModel : BaseNumberEditorViewModel<long>
{
    public Int64EditorViewModel(Setter<long> setter)
        : base(setter)
    {
        Value = setter.GetObservable()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
    }

    public ReadOnlyReactivePropertySlim<long> Value { get; }

    public override long Maximum => Setter.GetValueOrDefault(PropertyMetaTableKeys.Maximum, long.MaxValue);

    public override long Minimum => Setter.GetValueOrDefault(PropertyMetaTableKeys.Minimum, long.MinValue);

    public override INumberEditorService<long> EditorService { get; } = NumberEditorService.Instance.Get<long>();
}
