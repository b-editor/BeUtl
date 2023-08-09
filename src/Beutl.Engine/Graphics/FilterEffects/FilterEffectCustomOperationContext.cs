﻿using System.Diagnostics.CodeAnalysis;

using Beutl.Media.Source;

using SkiaSharp;

namespace Beutl.Graphics.Effects;

public class FilterEffectCustomOperationContext
{
    private readonly ImmediateCanvas _canvas;
    private EffectTarget _target;

    public FilterEffectCustomOperationContext(ImmediateCanvas canvas, EffectTarget target)
    {
        Target = target.Clone();
        _canvas = canvas;
    }

    public EffectTarget Target
    {
        get => _target;
        [MemberNotNull(nameof(_target))]
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _target = value;
        }
    }

    public void ReplaceTarget(EffectTarget target)
    {
        _target.Dispose();
        Target = target.Clone();
    }

    public EffectTarget CreateTarget(int width, int height)
    {
        SKSurface? surface = _canvas.CreateRenderTarget(width, height);
        if (surface != null)
        {
            using var surfaceRef = Ref<SKSurface>.Create(surface);
            return new EffectTarget(surfaceRef, new Size(width, height));
        }
        else
        {
            return EffectTarget.Empty;
        }
    }

    public ImmediateCanvas Open(EffectTarget target)
    {
        if (target.Surface == null)
        {
            throw new InvalidOperationException("無効なEffectTarget");
        }

        return _canvas.CreateCanvas(target.Surface.Value, true);
    }
}