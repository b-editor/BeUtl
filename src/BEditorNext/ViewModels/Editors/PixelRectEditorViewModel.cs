﻿using BEditorNext.Media;
using BEditorNext.ProjectSystem;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace BEditorNext.ViewModels.Editors;

public sealed class PixelRectEditorViewModel : BaseEditorViewModel<PixelRect>
{
    public PixelRectEditorViewModel(Setter<PixelRect> setter)
        : base(setter)
    {
        Value = setter.GetObservable()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
    }

    public ReadOnlyReactivePropertySlim<PixelRect> Value { get; }

    public PixelRect Maximum => Setter.GetValueOrDefault(PropertyMetaTableKeys.Maximum, new PixelRect(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));

    public PixelRect Minimum => Setter.GetValueOrDefault(PropertyMetaTableKeys.Minimum, new PixelRect(int.MinValue, int.MinValue, int.MinValue, int.MinValue));
}
