﻿using System.Collections;
using System.Collections.Specialized;
using System.Text.Json.Nodes;

using Beutl.Collections;
using Beutl.Media;
using Beutl.Styling;

namespace Beutl.Animation;

public class Animation<T> : BaseAnimation, IAnimation
{
    private static Animator<T>? s_animator;
    private readonly AnimationChildren _children;

    public Animation(CoreProperty<T> property)
        : base(property)
    {
        _children = new AnimationChildren(this);
        _children.Invalidated += (_, e) => Invalidated?.Invoke(this, e);
    }

    public new CoreProperty<T> Property => (CoreProperty<T>)base.Property;

    public ICoreList<AnimationSpan<T>> Children => _children;

    ICoreReadOnlyList<IAnimationSpan> IAnimation.Children => _children;

    public event EventHandler<RenderInvalidatedEventArgs>? Invalidated;

    public void ReadFromJson(JsonNode json)
    {
        if (json is JsonArray childrenArray)
        {
            _children.Clear();
            _children.EnsureCapacity(childrenArray.Count);

            foreach (JsonObject childJson in childrenArray.OfType<JsonObject>())
            {
                var item = new AnimationSpan<T>();
                item.ReadFromJson(childJson);
                _children.Add(item);
            }
        }
    }

    public void WriteToJson(ref JsonNode json)
    {
        var array = new JsonArray();

        foreach (AnimationSpan<T> item in _children.GetMarshal().Value)
        {
            JsonNode node = new JsonObject();
            item.WriteToJson(ref node);

            array.Add(node);
        }

        json = array;
    }

    public T Interpolate(TimeSpan timeSpan)
    {
        TimeSpan cur = TimeSpan.Zero;
        Span<AnimationSpan<T>> span = _children.GetMarshal().Value;
        if (span.Length == 0)
        {
            return GetAnimator().DefaultValue();
        }

        foreach (AnimationSpan<T> item in span)
        {
            TimeSpan next = cur + item.Duration;
            if (cur <= timeSpan && timeSpan < next)
            {
                // 相対的なTimeSpan
                TimeSpan time = timeSpan - cur;
                return item.Interpolate((float)(time / item.Duration));
            }
            else
            {
                cur = next;
            }
        }

        return span[^1].Interpolate(1);
    }

    public void ApplyTo(ICoreObject obj, TimeSpan ts)
    {
        if (_children.Count > 0)
        {
            obj.SetValue(Property, Interpolate(ts));
        }
    }

    private static Animator<T> GetAnimator()
    {
        return s_animator ??= (Animator<T>)Activator.CreateInstance(AnimatorRegistry.GetAnimatorType(typeof(T)))!;
    }

    private sealed class AnimationChildren : CoreList<AnimationSpan<T>>
    {
        public Animation<T> Parent { get; }

        public AnimationChildren(Animation<T> parent)
        {
            Parent = parent;
            ResetBehavior = ResetBehavior.Remove;
            CollectionChanged += AffectsRenders_CollectionChanged;
        }

        private void AffectsRenders_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            void AddHandlers(IList list)
            {
                foreach (IAnimationSpan? item in list.OfType<IAnimationSpan>())
                {
                    item.Invalidated += Item_Invalidated;
                }
            }

            void RemoveHandlers(IList list)
            {
                foreach (IAnimationSpan? item in list.OfType<IAnimationSpan>())
                {
                    item.Invalidated -= Item_Invalidated;
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                    AddHandlers(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                    RemoveHandlers(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace when e.NewItems is not null && e.OldItems is not null:
                    AddHandlers(e.NewItems);
                    RemoveHandlers(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                default:
                    break;
            }

            RaiseInvalidated(new RenderInvalidatedEventArgs(this));
        }

        public event EventHandler<RenderInvalidatedEventArgs>? Invalidated;

        private void Item_Invalidated(object? sender, RenderInvalidatedEventArgs e)
        {
            RaiseInvalidated(e);
        }

        private void RaiseInvalidated(RenderInvalidatedEventArgs args)
        {
            Invalidated?.Invoke(this, args);
        }
    }
}
