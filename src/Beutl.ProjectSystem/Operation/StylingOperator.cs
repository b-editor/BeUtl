﻿using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json.Nodes;

using Beutl.Animation;
using Beutl.Audio;
using Beutl.Extensibility;
using Beutl.Graphics;
using Beutl.Media;
using Beutl.Reactive;
using Beutl.Styling;

using static Beutl.Operation.StylingOperatorPropertyDefinition;

namespace Beutl.Operation;

public interface IStylingSetterPropertyImpl : IAbstractProperty
{
    ISetter Setter { get; }

    IStyle Style { get; }
}

public sealed class StylingSetterPropertyImpl<T> : IAbstractAnimatableProperty<T>, IStylingSetterPropertyImpl
{
    private sealed class AnimationObservable : LightweightObservableBase<IAnimation<T>?>
    {
        private readonly Setter<T> _setter;
        private IAnimation<T>? _prevAnimation;

        public AnimationObservable(Setter<T> setter)
        {
            _setter = setter;
        }

        protected override void Subscribed(IObserver<IAnimation<T>?> observer, bool first)
        {
            base.Subscribed(observer, first);
            observer.OnNext(_setter.Animation);
        }

        protected override void Deinitialize()
        {
            _setter.Invalidated -= Setter_Invalidated;
        }

        protected override void Initialize()
        {
            _setter.Invalidated += Setter_Invalidated;
        }

        private void Setter_Invalidated(object? sender, EventArgs e)
        {
            if (_prevAnimation != _setter.Animation)
            {
                PublishNext(_setter.Animation);
                _prevAnimation = _setter.Animation;
            }
        }
    }

    public StylingSetterPropertyImpl(Setter<T> setter, Style style)
    {
        Property = setter.Property;
        Setter = setter;
        Style = style;
        ObserveAnimation = new AnimationObservable(setter);
    }

    public CoreProperty<T> Property { get; }

    public Setter<T> Setter { get; }

    public Style Style { get; }

    public IAnimation<T>? Animation
    {
        get => Setter.Animation;
        set => Setter.Animation = value;
    }

    public IObservable<IAnimation<T>?> ObserveAnimation { get; }

    public Type PropertyType => Property.PropertyType;

    public string DisplayName
    {
        get
        {
            CorePropertyMetadata metadata = Property.GetMetadata<CorePropertyMetadata>(ImplementedType);
            return metadata.DisplayAttribute?.GetName() ?? Property.Name;
        }
    }

    public bool IsReadOnly => false;

    public Type ImplementedType => Style.TargetType;

    CoreProperty? IAbstractProperty.GetCoreProperty() => Property;

    ISetter IStylingSetterPropertyImpl.Setter => Setter;

    IStyle IStylingSetterPropertyImpl.Style => Style;

    public IObservable<T?> GetObservable()
    {
        return Setter;
    }

    public T? GetValue()
    {
        return Setter.Value;
    }

    public void SetValue(T? value)
    {
        Setter.Value = value;
    }

    public object? GetDefaultValue()
    {
        return Property.GetMetadata<ICorePropertyMetadata>(ImplementedType).GetDefaultValue();
    }
}

internal static class StylingOperatorPropertyDefinition
{
    internal record struct Definition(PropertyInfo Property, Func<object, ISetter> Getter, Action<object, ISetter> Setter);

    private static readonly Dictionary<Type, Definition[]> s_defines = new();

    public static ISetter[] GetSetters(object obj)
    {
        Type type = obj.GetType();
        if (!s_defines.TryGetValue(type, out Definition[]? def))
        {
            def = CreateDefinitions(type);
        }

        var array = new ISetter[def.Length];
        for (int i = 0; i < def.Length; i++)
        {
            array[i] = def[i].Getter(obj);
        }

        return array;
    }

    public static Definition[] GetDefintions(Type type)
    {
        if (!s_defines.TryGetValue(type, out Definition[]? def))
        {
            def = CreateDefinitions(type);
        }

        return def;
    }

    private static Definition[] CreateDefinitions(Type type)
    {
        PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance);
        var list = new List<(Definition, DisplayAttribute?)>();

        foreach (PropertyInfo item in props)
        {
            if (item.PropertyType.IsAssignableTo(typeof(ISetter))
                && item.CanWrite
                && item.CanRead)
            {
                DisplayAttribute? att = item.GetCustomAttribute<DisplayAttribute>();
                ParameterExpression target = Expression.Parameter(typeof(object), "target");
                ParameterExpression value = Expression.Parameter(typeof(ISetter), "value");

                // (object target) => (target as Type).Property;
                var getExpr = Expression.Lambda<Func<object, ISetter>>(
                    Expression.Property(Expression.TypeAs(target, type), item),
                    target);
                // (object target, ISetter value) => (target as Type).Property = value;
                var setExpr = Expression.Lambda<Action<object, ISetter>>(
                    Expression.Assign(Expression.Property(Expression.TypeAs(target, type), item), Expression.TypeAs(value, item.PropertyType)),
                    target, value);

                list.Add((new Definition(item, getExpr.Compile(), setExpr.Compile()), att));
            }
        }

        list.Sort((x, y) =>
        {
            int? xOrder = x.Item2?.GetOrder();
            int? yOrder = y.Item2?.GetOrder();
            if (xOrder == yOrder)
            {
                return 0;
            }

            if (!xOrder.HasValue)
            {
                return 1;
            }
            else if (!yOrder.HasValue)
            {
                return -1;
            }
            else
            {
                return xOrder.Value - yOrder.Value;
            }
        });

        Definition[] array = list.Select(x => x.Item1).ToArray();
        s_defines[type] = array;
        return array;
    }
}

public abstract class StylingOperator : SourceOperator
{
    private bool _isSettersChanging;
    private Style _style;
    private EvaluationTarget _preferredEvalTarget;

    protected StylingOperator()
    {
        Style = OnInitializeStyle(() => GetSetters(this));
    }

    public Style Style
    {
        get => _style;

        [MemberNotNull(nameof(_style))]
        private set
        {
            if (!ReferenceEquals(value, _style))
            {
                Properties.CollectionChanged -= Properties_CollectionChanged;
                if (_style != null)
                {
                    _style.Invalidated -= OnInvalidated;
                    _style.Setters.CollectionChanged -= Setters_CollectionChanged;
                    Properties.Clear();
                    _preferredEvalTarget = default;
                }

                _style = value;

                if (value != null)
                {
                    if (value.TargetType.IsAssignableTo(typeof(Drawable)))
                    {
                        _preferredEvalTarget = EvaluationTarget.Graphics;
                    }
                    else if (value.TargetType.IsAssignableTo(typeof(Sound)))
                    {
                        _preferredEvalTarget = EvaluationTarget.Audio;
                    }

                    value.Invalidated += OnInvalidated;
                    value.Setters.CollectionChanged += Setters_CollectionChanged;
                    Type propType = typeof(StylingSetterPropertyImpl<>);
                    Properties.AddRange(value.Setters.OfType<ISetter>()
                        .Select(x =>
                        {
                            Type type = propType.MakeGenericType(x.Property.PropertyType);
                            return (IAbstractProperty)Activator.CreateInstance(type, x, _style)!;
                        }));
                }

                Properties.CollectionChanged += Properties_CollectionChanged;
                //Instance = null;
            }
        }
    }

    public override EvaluationTarget GetEvaluationTarget() => _preferredEvalTarget;

    protected abstract Style OnInitializeStyle(Func<IList<ISetter>> setters);

    private void OnInvalidated(object? s, EventArgs e)
    {
        RaiseInvalidated(new RenderInvalidatedEventArgs(this, nameof(Style)));
    }

    public override void ReadFromJson(JsonObject json)
    {
        base.ReadFromJson(json);

        Definition[] defs = GetDefintions(GetType());
        foreach (Definition item in defs.AsSpan())
        {
            string name = item.Property.Name;
            if (json.TryGetPropertyValue(name, out JsonNode? propNode))
            {
                if (propNode == null)
                {

                }

                ISetter knownSetter = item.Getter.Invoke(this);

                if (propNode.ToSetter(knownSetter.Property.Name, _style.TargetType) is ISetter setter)
                {
                    item.Setter.Invoke(this, setter);
                }
            }
        }

        Style = OnInitializeStyle(() => GetSetters(this));
        RaiseInvalidated(new RenderInvalidatedEventArgs(this));
    }

    public override void WriteToJson(JsonObject json)
    {
        base.WriteToJson(json);
        Definition[] defs = GetDefintions(GetType());
        foreach (Definition item in defs.AsSpan())
        {
            string name = item.Property.Name;
            ISetter setter = item.Getter.Invoke(this);
            json[name] = setter.ToJson(_style.TargetType).Item2;
        }
    }

    private void Properties_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_isSettersChanging)
        {
            throw new InvalidOperationException(
                "If you inherit from 'StylingOperator', you cannot change 'Properties' directly; you must do so from 'Style.Setters'.");
        }
    }

    private void Setters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        void Add(int index, IList list)
        {
            Type propType = typeof(StylingSetterPropertyImpl<>);

            Properties.InsertRange(index, list.OfType<ISetter>()
                .Select(x =>
                {
                    Type type = propType.MakeGenericType(x.Property.PropertyType);
                    return (IAbstractProperty)Activator.CreateInstance(type, x, Style)!;
                }));
        }

        void Remove(int index, IList list)
        {
            Properties.RemoveRange(index, list.Count);
        }

        try
        {
            _isSettersChanging = true;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add(e.NewStartingIndex, e.NewItems!);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Remove(e.OldStartingIndex, e.OldItems!);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    Remove(e.OldStartingIndex, e.OldItems!);
                    int newIndex = e.NewStartingIndex;
                    if (newIndex > e.OldStartingIndex)
                    {
                        newIndex -= e.OldItems!.Count;
                    }
                    Add(newIndex, e.NewItems!);
                    break;

                case NotifyCollectionChangedAction.Move:
                    Properties.MoveRange(e.OldStartingIndex, e.NewItems!.Count, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Properties.Clear();
                    break;

                default:
                    break;
            }

            if (sender is ICollection collection)
            {
                RaiseInvalidated(new RenderInvalidatedEventArgs(collection));
            }
        }
        finally
        {
            _isSettersChanging = false;
        }
    }
}
