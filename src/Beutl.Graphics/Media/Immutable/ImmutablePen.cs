﻿namespace Beutl.Media.Immutable;

public sealed class ImmutablePen : IPen, IEquatable<IPen?>
{
    public ImmutablePen(
        IBrush? brush,
        IReadOnlyList<float>? dashArray,
        float dashOffset,
        float thickness,
        float miterLimit,
        StrokeCap strokeCap,
        StrokeJoin strokeJoin,
        StrokeAlignment strokeAlignment)
    {
        Brush = brush;
        DashArray = dashArray;
        DashOffset = dashOffset;
        Thickness = thickness;
        MiterLimit = miterLimit;
        StrokeCap = strokeCap;
        StrokeJoin = strokeJoin;
        StrokeAlignment = strokeAlignment;
    }

    public IBrush? Brush { get; }

    public IReadOnlyList<float>? DashArray { get; }

    public float DashOffset { get; }

    public float Thickness { get; }
    
    public float MiterLimit { get; }

    public StrokeCap StrokeCap { get; }

    public StrokeJoin StrokeJoin { get; }

    public StrokeAlignment StrokeAlignment { get; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as IPen);
    }

    public bool Equals(IPen? other)
    {
        return other is not null
            && EqualityComparer<IBrush?>.Default.Equals(Brush, other.Brush)
            && EqualityComparer<IReadOnlyList<float>?>.Default.Equals(DashArray, other.DashArray)
            && DashOffset == other.DashOffset
            && Thickness == other.Thickness
            && MiterLimit == other.MiterLimit
            && StrokeCap == other.StrokeCap
            && StrokeJoin == other.StrokeJoin
            && StrokeAlignment == other.StrokeAlignment;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Brush, DashArray, DashOffset, Thickness, MiterLimit, StrokeCap, StrokeJoin, StrokeAlignment);
    }

    public static bool operator ==(ImmutablePen? left, ImmutablePen? right) => EqualityComparer<ImmutablePen>.Default.Equals(left, right);

    public static bool operator !=(ImmutablePen? left, ImmutablePen? right) => !(left == right);
}