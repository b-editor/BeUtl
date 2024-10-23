﻿using Beutl.Media;
using Beutl.Styling;

namespace Beutl.Graphics.Rendering;

public abstract class Renderable : Styleable, IAffectsRender
{
    public static readonly CoreProperty<bool> IsVisibleProperty;
    public static readonly CoreProperty<int> ZIndexProperty;
    public static readonly CoreProperty<TimeRange> TimeRangeProperty;
    private bool _isVisible = true;
    private int _zIndex;
    private TimeRange _timeRange;

    public event EventHandler<RenderInvalidatedEventArgs>? Invalidated;

    static Renderable()
    {
        IsVisibleProperty = ConfigureProperty<bool, Renderable>(nameof(IsVisible))
            .Accessor(o => o.IsVisible, (o, v) => o.IsVisible = v)
            .DefaultValue(true)
            .Register();

        ZIndexProperty = ConfigureProperty<int, Renderable>(nameof(ZIndex))
            .Accessor(o => o.ZIndex, (o, v) => o.ZIndex = v)
            .Register();

        TimeRangeProperty = ConfigureProperty<TimeRange, Renderable>(nameof(TimeRange))
            .Accessor(o => o.TimeRange, (o, v) => o.TimeRange = v)
            .Register();

        AffectsRender<Renderable>(IsVisibleProperty);
    }

    protected Renderable()
    {
        AnimationInvalidated += (_, e) => RaiseInvalidated(e);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetAndRaise(IsVisibleProperty, ref _isVisible, value);
    }

    public int ZIndex
    {
        get => _zIndex;
        set => SetAndRaise(ZIndexProperty, ref _zIndex, value);
    }

    public TimeRange TimeRange
    {
        get => _timeRange;
        set => SetAndRaise(TimeRangeProperty, ref _timeRange, value);
    }

    internal int Version { get; private set; }

    private void AffectsRender_Invalidated(object? sender, RenderInvalidatedEventArgs e)
    {
        RaiseInvalidated(e);
    }

    protected static void AffectsRender<T>(params CoreProperty[] properties)
        where T : Renderable
    {
        foreach (CoreProperty item in properties)
        {
            item.Changed.Subscribe(e =>
            {
                if (e.Sender is T s)
                {
                    s.RaiseInvalidated(new RenderInvalidatedEventArgs(s, e.Property.Name));

                    if (e.OldValue is IAffectsRender oldAffectsRender)
                    {
                        oldAffectsRender.Invalidated -= s.AffectsRender_Invalidated;
                    }

                    if (e.NewValue is IAffectsRender newAffectsRender)
                    {
                        newAffectsRender.Invalidated += s.AffectsRender_Invalidated;
                    }
                }
            });
        }
    }

    public void Invalidate()
    {
        RaiseInvalidated(new RenderInvalidatedEventArgs(this));
    }

    protected void RaiseInvalidated(RenderInvalidatedEventArgs args)
    {
        Invalidated?.Invoke(this, args);
        unchecked
        {
            Version++;
        }
    }
}
