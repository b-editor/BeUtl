﻿#pragma warning disable CS0618

using Beutl.Graphics;
using Beutl.Graphics.Effects;
using Beutl.Media;

namespace Beutl.UnitTests.Engine.Graphics.Effects;

public class StrokeEffectTest
{
    [Test]
    public void StrokeEffect_ShouldHaveDefaultValues()
    {
        var effect = new StrokeEffect();

        Assert.That(effect.Pen, Is.Not.Null);
        Assert.That(effect.Offset, Is.EqualTo(new Point()));
        Assert.That(effect.Style, Is.EqualTo(Border.BorderStyles.Background));
    }

    [Test]
    public void StrokeEffect_ShouldUpdateProperties()
    {
        var effect = new StrokeEffect();
        effect.Pen = new Pen { Brush = Brushes.Red, Thickness = 2 };
        effect.Offset = new Point(10, 10);
        effect.Style = Border.BorderStyles.Foreground;

        Assert.That(effect.Pen, Is.Not.Null);
        Assert.That(effect.Offset, Is.EqualTo(new Point(10, 10)));
        Assert.That(effect.Style, Is.EqualTo(Border.BorderStyles.Foreground));
    }

    [Test]
    public void StrokeEffect_ShouldApplyToContext()
    {
        var effect = new StrokeEffect();
        effect.Pen = new Pen { Brush = Brushes.Red, Thickness = 2 };
        effect.Offset = new Point(10, 10);
        effect.Style = Border.BorderStyles.Foreground;
        using var context = new FilterEffectContext(new Rect(0, 0, 100, 100));

        context.Apply(effect);

        // 適用結果の検証
        Assert.That(context._items, Is.Not.Empty);
        Assert.That(context._items[0].FilterEffect, Is.EqualTo(effect));
        Assert.That(context._items[0].Item, Is.InstanceOf<IFEItem_Custom>());
    }
}
