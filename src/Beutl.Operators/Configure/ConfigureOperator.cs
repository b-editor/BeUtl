﻿using System.Text.Json.Nodes;

using Beutl.Animation;
using Beutl.Framework;
using Beutl.Media;
using Beutl.Rendering;
using Beutl.Streaming;

namespace Beutl.Operators.Configure;

public abstract class ConfigureOperator<TTarget, TValue> : StreamOperator, IStreamSelector
    where TTarget : IRenderable
    where TValue : CoreObject, IAffectsRender, new()
{
    private bool _selecting;

    public ConfigureOperator()
    {
        Value = new TValue();
        Value.Invalidated += (_, e) =>
        {
            if (!_selecting)
            {
                RaiseInvalidated(e);
            }
        };

        Type anmPropType = typeof(AnimatableCorePropertyImpl<>);
        Type propType = typeof(CorePropertyImpl<>);
        Type ownerType = typeof(TTarget);
        bool isAnimatable = Value is IAnimatable;

        Properties.AddRange(GetProperties().Select(x =>
        {
            Type propTypeFact = (isAnimatable && x.GetMetadata<CorePropertyMetadata>(ownerType).PropertyFlags.HasFlag(PropertyFlags.Animatable)
                ? anmPropType
                : propType).MakeGenericType(x.PropertyType);

            return (IAbstractProperty)Activator.CreateInstance(propTypeFact, x, Value)!;
        }));
    }

    protected TValue Value { get; }

    protected TTarget? Previous { get; set; }

    public IRenderable? Select(IRenderable? value, IClock clock)
    {
        try
        {
            _selecting = true;
            if (value is TTarget current)
            {
                PreSelect(current, Value);
                if (!ReferenceEquals(Previous, current))
                {
                    if (Previous != null)
                    {
                        OnDetached(Previous, Value);
                    }

                    OnAttached(current, Value);
                    Previous = current;
                }

                PostSelect(current, Value);
            }
            else if (Previous != null)
            {
                OnDetached(Previous, Value);
                Previous = default;
            }

            return value;
        }
        finally
        {
            _selecting = false;
        }
    }

    public override void ReadFromJson(JsonNode json)
    {
        base.ReadFromJson(json);
        if (json is JsonObject jobj
            && jobj.TryGetPropertyValue("value", out JsonNode? node)
            && node != null)
        {
            Value.ReadFromJson(node);
        }
    }

    public override void WriteToJson(ref JsonNode json)
    {
        base.WriteToJson(ref json);
        if (json is JsonObject jobj)
        {
            JsonNode node = new JsonObject();
            Value.WriteToJson(ref node);
            jobj["value"] = node;
        }
    }

    protected virtual void PreSelect(TTarget target, TValue value)
    {
    }

    protected virtual void PostSelect(TTarget target, TValue value)
    {
    }

    protected abstract void OnAttached(TTarget target, TValue value);

    protected abstract void OnDetached(TTarget target, TValue value);

    protected override void OnDetachedFromLogicalTree(in LogicalTreeAttachmentEventArgs args)
    {
        base.OnDetachedFromLogicalTree(args);
        if (Previous is { } previous)
        {
            OnDetached(previous, Value);
            Previous = default;
        }
    }

    protected virtual IEnumerable<CoreProperty> GetProperties()
    {
        yield break;
    }
}
