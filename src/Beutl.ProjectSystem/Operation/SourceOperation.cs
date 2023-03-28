﻿using System.Buffers;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

using Avalonia.Collections.Pooled;

using Beutl.Collections;
using Beutl.Media;
using Beutl.ProjectSystem;
using Beutl.Rendering;

namespace Beutl.Operation;

public sealed class SourceOperation : Hierarchical, IAffectsRender
{
    private readonly HierarchicalList<SourceOperator> _children;
    private OperatorEvaluationContext[]? _contexts;
    private int _contextsLength;
    private bool _isDirty = true;

    public SourceOperation()
    {
        _children = new HierarchicalList<SourceOperator>(this);
        _children.Attached += OnOperatorAttached;
        _children.Detached += OnOperatorDetached;
        _children.CollectionChanged += OnOperatorsCollectionChanged;
    }

    public event EventHandler<RenderInvalidatedEventArgs>? Invalidated;

    public ICoreList<SourceOperator> Children => _children;

    public override void ReadFromJson(JsonNode json)
    {
        base.ReadFromJson(json);

        if (json is JsonObject jobject)
        {
            if (jobject.TryGetPropertyValue("children", out JsonNode? childrenNode)
                && childrenNode is JsonArray childrenArray)
            {
                foreach (JsonObject operatorJson in childrenArray.OfType<JsonObject>())
                {
                    if (operatorJson.TryGetPropertyValue("@type", out JsonNode? atTypeNode)
                        && atTypeNode is JsonValue atTypeValue
                        && atTypeValue.TryGetValue(out string? atType))
                    {
                        var type = TypeFormat.ToType(atType);
                        SourceOperator? @operator = null;

                        if (type?.IsAssignableTo(typeof(SourceOperator)) ?? false)
                        {
                            @operator = Activator.CreateInstance(type) as SourceOperator;
                        }

                        @operator ??= new SourceOperator();
                        @operator.ReadFromJson(operatorJson);
                        Children.Add(@operator);
                    }
                }
            }

        }
    }

    public override void WriteToJson(ref JsonNode json)
    {
        base.WriteToJson(ref json);

        if (json is JsonObject jobject)
        {
            Span<SourceOperator> children = _children.GetMarshal().Value;
            if (children.Length > 0)
            {
                var array = new JsonArray();

                foreach (SourceOperator item in children)
                {
                    JsonNode node = new JsonObject();
                    item.WriteToJson(ref node);
                    node["@type"] = TypeFormat.ToString(item.GetType());

                    array.Add(node);
                }

                jobject["children"] = array;
            }

        }
    }

    public void Evaluate(IRenderer renderer, Layer layer, IList<Renderable> unhandled)
    {
        void Detach(IList<Renderable> renderables)
        {
            foreach (Renderable item in renderables)
            {
                if (item.HierarchicalParent is RenderLayerSpan span
                    && layer.Span != span)
                {
                    span.Value.Remove(item);
                }
            }
        }

        layer.Span.Value.Clear();

        Initialize(renderer);
        if (_contexts != null)
        {
            if (!layer.AllowOutflow)
            {
                using var flow = new PooledList<Renderable>();
                foreach (OperatorEvaluationContext? item in _contexts.AsSpan().Slice(0, _contextsLength))
                {
                    item.FlowRenderables = flow;
                    item.Operator.Evaluate(item);
                }

                Detach(flow);
                layer.Span.Value.AddRange(flow);
            }
            else
            {
                using var pooled = new PooledList<Renderable>();
                foreach (OperatorEvaluationContext? item in _contexts.AsSpan().Slice(0, _contextsLength))
                {
                    item._renderables = pooled;
                    item.FlowRenderables = unhandled;
                    item.Operator.Evaluate(item);
                }

                Detach(pooled);
                layer.Span.Value.AddRange(pooled);
            }

            foreach (Renderable item in layer.Span.Value.GetMarshal().Value)
            {
                item.ApplyStyling(renderer.Clock);
                item.ApplyAnimations(renderer.Clock);
                item.IsVisible = layer.IsEnabled;
                while (item.BatchUpdate)
                {
                    item.EndBatchUpdate();
                }
            }
        }
        else
        {
            layer.Span.Value.Clear();
        }
    }

    [MemberNotNull(nameof(_contexts))]
    private Memory<OperatorEvaluationContext> RentContextsArray(int size)
    {
        if (_contexts != null)
            throw new InvalidOperationException("ReturnContextsArray has not yet been called.");

        _contexts = ArrayPool<OperatorEvaluationContext>.Shared.Rent(size);
        _contextsLength = size;

        return _contexts.AsMemory().Slice(0, size);
    }

    private void ReturnContextsArray()
    {
        if (_contexts != null)
        {
            ArrayPool<OperatorEvaluationContext>.Shared.Return(_contexts, true);
            _contexts = null;
            _contextsLength = -1;
        }
    }

    private void Uninitialize()
    {
        if (_contexts != null)
        {
            foreach (OperatorEvaluationContext? item in _contexts.AsSpan().Slice(0, _contextsLength))
            {
                item.Operator.UninitializeForContext(item);
            }

            ReturnContextsArray();
        }
    }

    private void Initialize(IRenderer renderer)
    {
        if (_isDirty)
        {
            Uninitialize();
            Span<OperatorEvaluationContext> contexts = RentContextsArray(Children.Count).Span;

            int index = 0;
            foreach (SourceOperator item in Children.GetMarshal().Value)
            {
                contexts[index++] = new OperatorEvaluationContext(item)
                {
                    Clock = renderer.Clock,
                    Renderer = renderer,
                    List = _contexts
                };
            }

            foreach (OperatorEvaluationContext item in contexts)
            {
                item.Operator.InitializeForContext(item);
            }

            _isDirty = false;
        }
    }

    public IRecordableCommand AddChild(SourceOperator @operator)
    {
        ArgumentNullException.ThrowIfNull(@operator);

        return Children.BeginRecord<SourceOperator>()
            .Add(@operator)
            .ToCommand();
    }

    public IRecordableCommand RemoveChild(SourceOperator @operator)
    {
        ArgumentNullException.ThrowIfNull(@operator);

        return Children.BeginRecord<SourceOperator>()
            .Remove(@operator)
            .ToCommand();
    }

    public IRecordableCommand InsertChild(int index, SourceOperator @operator)
    {
        ArgumentNullException.ThrowIfNull(@operator);

        return Children.BeginRecord<SourceOperator>()
            .Insert(index, @operator)
            .ToCommand();
    }

    private void OnOperatorsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _isDirty = true;
        Invalidated?.Invoke(this, new RenderInvalidatedEventArgs(this));
    }

    private void OnOperatorAttached(SourceOperator obj)
    {
        obj.Invalidated += OnOperatorInvalidated;
    }

    private void OnOperatorDetached(SourceOperator obj)
    {
        obj.Invalidated -= OnOperatorInvalidated;
    }

    private void OnOperatorInvalidated(object? sender, RenderInvalidatedEventArgs e)
    {
        Invalidated?.Invoke(this, e);
    }
}