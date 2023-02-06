﻿using System.Diagnostics.CodeAnalysis;

using Avalonia.Controls;

using Reactive.Bindings;

namespace Beutl.Framework;

public interface IToolContext : IDisposable, IJsonSerializable
{
    ToolTabExtension Extension { get; }

    IReactiveProperty<bool> IsSelected { get; }

    string Header => Extension.Header ?? "";

    ToolTabExtension.TabPlacement Placement { get; }
}

public abstract class ToolTabExtension : ViewExtension
{
    public enum TabPlacement
    {
        Bottom,
        Right
    }

    public abstract bool CanMultiple { get; }

    public virtual string? Header => null;

    public abstract bool TryCreateContent(
        IEditorContext editorContext,
        [NotNullWhen(true)] out Control? control);

    public abstract bool TryCreateContext(
        IEditorContext editorContext,
        [NotNullWhen(true)] out IToolContext? context);
}
