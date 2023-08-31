﻿using Beutl.Audio;
using Beutl.Audio.Effects;
using Beutl.Media.Source;
using Beutl.Operation;
using Beutl.Styling;

namespace Beutl.Operators.Source;

public sealed class SourceSoundOperator : StyledSourcePublisher
{
    public Setter<ISoundSource?> Source { get; set; }
        = new Setter<ISoundSource?>(SourceSound.SourceProperty, null);

    public Setter<float> Gain { get; set; }
        = new Setter<float>(Sound.GainProperty, 100);

    public Setter<ISoundEffect?> Effect { get; set; }
        = new Setter<ISoundEffect?>(Sound.EffectProperty, new SoundEffectGroup());

    protected override Style OnInitializeStyle(Func<IList<ISetter>> setters)
    {
        var style = new Style<SourceSound>();
        style.Setters.AddRange(setters());
        return style;
    }

    protected override void OnBeforeApplying()
    {
        base.OnBeforeApplying();
        if (Instance?.Target is SourceSound sound)
        {
            sound.BeginBatchUpdate();
        }
    }
}
