﻿using System.Diagnostics.CodeAnalysis;

using Avalonia.Controls;

using Beutl.Framework;
using Beutl.ViewModels;
using Beutl.ViewModels.Tools;
using Beutl.Views.Tools;

namespace Beutl.Services.PrimitiveImpls;

[PrimitiveImpl]
public sealed class AnimationTabExtension : ToolTabExtension
{
    public static readonly AnimationTabExtension Instance = new();

    public override bool CanMultiple => false;

    public override string Name => "Animation";

    public override string DisplayName => "Animation";

    public override string? Header => Strings.Animation;

    public override bool TryCreateContent(IEditorContext editorContext, [NotNullWhen(true)] out Control? control)
    {
        if (editorContext is EditViewModel)
        {
            control = new AnimationTab();
            return true;
        }
        else
        {
            control = null;
            return false;
        }
    }

    public override bool TryCreateContext(IEditorContext editorContext, [NotNullWhen(true)] out IToolContext? context)
    {
        if (editorContext is EditViewModel)
        {
            context = new AnimationTabViewModel();
            return true;
        }
        else
        {
            context = null;
            return false;
        }
    }
}
