﻿using Beutl.Animation;
using Beutl.Styling;

namespace Beutl.Media;

/// <summary>
/// Describes the location and color of a transition point in a gradient.
/// </summary>
public sealed class GradientStop : Animatable, IGradientStop, IAffectsRender
{
    public static readonly CoreProperty<float> OffsetProperty;
    public static readonly CoreProperty<Color> ColorProperty;
    private float _offset;
    private Color _color;

    static GradientStop()
    {
        OffsetProperty = ConfigureProperty<float, GradientStop>(nameof(Offset))
            .Accessor(o => o.Offset, (o, v) => o.Offset = v)
            .DefaultValue(0)
            .PropertyFlags(PropertyFlags.All)
            .SerializeName("offset")
            .Register();

        ColorProperty = ConfigureProperty<Color, GradientStop>(nameof(Color))
            .Accessor(o => o.Color, (o, v) => o.Color = v)
            .DefaultValue(Colors.Transparent)
            .PropertyFlags(PropertyFlags.All)
            .SerializeName("color")
            .Register();

        static void OnChanged(CorePropertyChangedEventArgs obj)
        {
            if (obj.Sender is GradientStop s)
            {
                s.Invalidated?.Invoke(s, new RenderInvalidatedEventArgs(s, obj.Property.Name));
            }
        }

        OffsetProperty.Changed.Subscribe(OnChanged);
        ColorProperty.Changed.Subscribe(OnChanged);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GradientStop"/> class.
    /// </summary>
    public GradientStop() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GradientStop"/> class.
    /// </summary>
    /// <param name="color">The color</param>
    /// <param name="offset">The offset</param>
    public GradientStop(Color color, float offset)
    {
        Color = color;
        Offset = offset;
    }

    /// <inheritdoc/>
    public event EventHandler<RenderInvalidatedEventArgs>? Invalidated;

    /// <inheritdoc/>
    public float Offset
    {
        get => _offset;
        set => SetAndRaise(OffsetProperty, ref _offset, value);
    }

    /// <inheritdoc/>
    public Color Color
    {
        get => _color;
        set => SetAndRaise(ColorProperty, ref _color, value);
    }
}
