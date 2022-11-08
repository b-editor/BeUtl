﻿using Beutl.Graphics;
using Beutl.Streaming;
using Beutl.Styling;

namespace Beutl.Operators.Configure;

public sealed class BlendOperator : StreamStyler
{
    protected override Style OnInitializeStyle(Func<IList<ISetter>> setters)
    {
        var style = new Style<Drawable>();
        style.Setters.AddRange(setters());
        return style;
    }

    protected override void OnInitializeSetters(IList<ISetter> initializing)
    {
        initializing.Add(new Setter<BlendMode>(Drawable.BlendModeProperty, BlendMode.SrcOver));
    }
}