﻿using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json.Nodes;

using Beutl.Animation;
using Beutl.Language;
using Beutl.Media;
using Beutl.NodeTree;
using Beutl.Operation;
using Beutl.Rendering;

namespace Beutl.ProjectSystem;

public class Element : ProjectItem
{
    public static readonly CoreProperty<TimeSpan> StartProperty;
    public static readonly CoreProperty<TimeSpan> LengthProperty;
    public static readonly CoreProperty<int> ZIndexProperty;
    public static readonly CoreProperty<Color> AccentColorProperty;
    public static readonly CoreProperty<bool> IsEnabledProperty;
    public static readonly CoreProperty<bool> AllowOutflowProperty;
    public static readonly CoreProperty<RenderLayerSpan> SpanProperty;
    public static readonly CoreProperty<SourceOperation> OperationProperty;
    public static readonly CoreProperty<ElementNodeTreeModel> NodeTreeProperty;
    public static readonly CoreProperty<bool> UseNodeProperty;
    private readonly InstanceClock _instanceClock = new();
    private TimeSpan _start;
    private TimeSpan _length;
    private int _zIndex;
    private bool _isEnabled = true;
    private bool _allowOutflow = false;
    private IDisposable? _disposable;
    private bool _useNode;

    static Element()
    {
        StartProperty = ConfigureProperty<TimeSpan, Element>(nameof(Start))
            .Accessor(o => o.Start, (o, v) => o.Start = v)
            .Register();

        LengthProperty = ConfigureProperty<TimeSpan, Element>(nameof(Length))
            .Accessor(o => o.Length, (o, v) => o.Length = v)
            .Register();

        ZIndexProperty = ConfigureProperty<int, Element>(nameof(ZIndex))
            .Accessor(o => o.ZIndex, (o, v) => o.ZIndex = v)
            .Register();

        AccentColorProperty = ConfigureProperty<Color, Element>(nameof(AccentColor))
            .DefaultValue(Colors.Teal)
            .Register();

        IsEnabledProperty = ConfigureProperty<bool, Element>(nameof(IsEnabled))
            .Accessor(o => o.IsEnabled, (o, v) => o.IsEnabled = v)
            .DefaultValue(true)
            .Register();

        AllowOutflowProperty = ConfigureProperty<bool, Element>(nameof(AllowOutflow))
            .Accessor(o => o.AllowOutflow, (o, v) => o.AllowOutflow = v)
            .DefaultValue(false)
            .Register();

        SpanProperty = ConfigureProperty<RenderLayerSpan, Element>(nameof(Span))
            .Accessor(o => o.Span, null)
            .Register();

        OperationProperty = ConfigureProperty<SourceOperation, Element>(nameof(Operation))
            .Accessor(o => o.Operation, null)
            .Register();

        NodeTreeProperty = ConfigureProperty<ElementNodeTreeModel, Element>(nameof(NodeTree))
            .Accessor(o => o.NodeTree, null)
            .Register();

        UseNodeProperty = ConfigureProperty<bool, Element>(nameof(UseNode))
            .Accessor(o => o.UseNode, (o, v) => o.UseNode = v)
            .DefaultValue(false)
            .Register();

        ZIndexProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is Element layer && layer.HierarchicalParent is Scene { Renderer: { IsDisposed: false } renderer })
            {
                renderer[args.OldValue]?.RemoveSpan(layer.Span);
                if (args.NewValue >= 0)
                {
                    IRenderLayer? context = renderer[args.NewValue];
                    if (context == null)
                    {
                        context = new RenderLayer();
                        renderer[args.NewValue] = context;
                    }
                    context.AddSpan(layer.Span);
                }
            }
        });

        IsEnabledProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is Element layer)
            {
                layer.ForceRender();
            }
        });
        AllowOutflowProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is Element layer)
            {
                layer.ForceRender();
            }
        });
        UseNodeProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is Element layer)
            {
                layer.ForceRender();
            }
        });

        StartProperty.Changed.Subscribe(e =>
        {
            if (e.Sender is Element layer)
            {
                layer.Span.Start = e.NewValue;
            }
        });

        LengthProperty.Changed.Subscribe(e =>
        {
            if (e.Sender is Element layer)
            {
                layer.Span.Duration = e.NewValue;
            }
        });
    }

    public Element()
    {
        Operation = new SourceOperation();
        Operation.Invalidated += (_, _) => ForceRender();
#if DEBUG
        Operation.Children.CollectionChanged += OnOperatorsCollectionChanged;
#endif
        NodeTree = new ElementNodeTreeModel();
        NodeTree.Invalidated += (_, _) => ForceRender();

        HierarchicalChildren.Add(Operation);
        HierarchicalChildren.Add(Span);
        HierarchicalChildren.Add(NodeTree);
    }

    private void OnOperatorsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateName();
    }

    [Conditional("DEBUG")]
    private void UpdateName()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Operation.Children.Count; i++)
        {
            SourceOperator op = Operation.Children[i];
            if (op.IsEnabled)
            {
                Type type = op.GetType();
                string name = OperatorRegistry.FindItem(type)?.DisplayName ?? type.Name;

                sb.Append($"{name}, ");
            }
        }

        Name = sb.ToString();
    }

    // 0以上
    [Display(Name = nameof(Strings.StartTime), ResourceType = typeof(Strings))]
    public TimeSpan Start
    {
        get => _start;
        set => SetAndRaise(StartProperty, ref _start, value);
    }

    [Display(Name = nameof(Strings.DurationTime), ResourceType = typeof(Strings))]
    public TimeSpan Length
    {
        get => _length;
        set => SetAndRaise(LengthProperty, ref _length, value);
    }

    public TimeRange Range => new(Start, Length);

    public int ZIndex
    {
        get => _zIndex;
        set => SetAndRaise(ZIndexProperty, ref _zIndex, value);
    }

    public Color AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetAndRaise(IsEnabledProperty, ref _isEnabled, value);
    }

    public bool AllowOutflow
    {
        get => _allowOutflow;
        set => SetAndRaise(AllowOutflowProperty, ref _allowOutflow, value);
    }

    public RenderLayerSpan Span { get; } = new();

    public SourceOperation Operation { get; }

    public ElementNodeTreeModel NodeTree { get; }

    public bool UseNode
    {
        get => _useNode;
        set => SetAndRaise(UseNodeProperty, ref _useNode, value);
    }

    public IClock Clock => _instanceClock;

    protected override void SaveCore(string filename)
    {
        this.JsonSave(filename);
    }

    protected override void RestoreCore(string filename)
    {
        this.JsonRestore(filename);
    }

    public override void ReadFromJson(JsonObject json)
    {
        base.ReadFromJson(json);

        if (json.TryGetPropertyValue(nameof(Operation), out JsonNode? operationNode)
            && operationNode is JsonObject operationObj)
        {
            Operation.ReadFromJson(operationObj);
        }

        if (json.TryGetPropertyValue(nameof(NodeTree), out JsonNode? nodeTreeNode)
            && nodeTreeNode is JsonObject nodeTreeObj)
        {
            NodeTree.ReadFromJson(nodeTreeObj);
        }
    }

    public override void WriteToJson(JsonObject json)
    {
        base.WriteToJson(json);

        var operationJson = new JsonObject();
        Operation.WriteToJson(operationJson);
        json[nameof(Operation)] = operationJson;

        var nodeTreeJson = new JsonObject();
        NodeTree.WriteToJson(nodeTreeJson);
        json[nameof(NodeTree)] = nodeTreeJson;
    }

    public void Evaluate(IRenderer renderer, List<Renderable> unhandleds)
    {
        _instanceClock.GlobalClock = renderer.Clock;
        _instanceClock.BeginTime = Start;
        _instanceClock.DurationTime = Length;
        _instanceClock.CurrentTime = renderer.Clock.CurrentTime - Start;
        _instanceClock.AudioStartTime = renderer.Clock.AudioStartTime - Start;
        if (UseNode)
        {
            NodeTree.Evaluate(renderer, this);
        }
        else
        {
            Operation.Evaluate(renderer, this, unhandleds);
        }
    }

    protected override void OnAttachedToHierarchy(in HierarchyAttachmentEventArgs args)
    {
        base.OnAttachedToHierarchy(args);
        if (args.Parent is Scene { Renderer: { IsDisposed: false } renderer } && ZIndex >= 0)
        {
            IRenderLayer? context = renderer[ZIndex];
            if (context == null)
            {
                context = new RenderLayer();
                renderer[ZIndex] = context;
            }
            context.AddSpan(Span);

            _disposable = SubscribeToLayerNode();
        }
    }

    protected override void OnDetachedFromHierarchy(in HierarchyAttachmentEventArgs args)
    {
        base.OnDetachedFromHierarchy(args);
        if (args.Parent is Scene { Renderer: { IsDisposed: false } renderer } && ZIndex >= 0)
        {
            renderer[ZIndex]?.RemoveSpan(Span);
            _disposable?.Dispose();
            _disposable = null;
        }
    }

    private void ForceRender()
    {
        Scene? scene = this.FindHierarchicalParent<Scene>();
        if (IsEnabled
            && scene != null
            && Start <= scene.CurrentFrame
            && scene.CurrentFrame < Start + Length
            && scene.Renderer is { IsDisposed: false, IsGraphicsRendering: false })
        {
            scene.Renderer.RaiseInvalidated(scene.CurrentFrame);
        }
    }

    private IDisposable SubscribeToLayerNode()
    {
        return Span.GetObservable(RenderLayerSpan.ValueProperty)
            .SelectMany(value => value != null
                ? Observable.FromEventPattern<RenderInvalidatedEventArgs>(h => value.Invalidated += h, h => value.Invalidated -= h)
                    .Select(_ => Unit.Default)
                    .Publish(Unit.Default)
                    .RefCount()
                : Observable.Return(Unit.Default))
            .Subscribe(_ => ForceRender());
    }

    internal Element? GetBefore(int zindex, TimeSpan start)
    {
        if (HierarchicalParent is Scene scene)
        {
            Element? tmp = null;
            foreach (Element? item in scene.Children.GetMarshal().Value)
            {
                if (item != this && item.ZIndex == zindex && item.Start < start)
                {
                    if (tmp == null || tmp.Start <= item.Start)
                    {
                        tmp = item;
                    }
                }
            }
            return tmp;
        }

        return null;
    }

    internal Element? GetAfter(int zindex, TimeSpan end)
    {
        if (HierarchicalParent is Scene scene)
        {
            Element? tmp = null;
            foreach (Element? item in scene.Children.GetMarshal().Value)
            {
                if (item != this && item.ZIndex == zindex && item.Range.End > end)
                {
                    if (tmp == null || tmp.Range.End >= item.Range.End)
                    {
                        tmp = item;
                    }
                }
            }
            return tmp;
        }

        return null;
    }

    internal (Element? Before, Element? After, Element? Cover) GetBeforeAndAfterAndCover(int zindex, TimeSpan start, TimeSpan end)
    {
        if (HierarchicalParent is Scene scene)
        {
            Element? beforeTmp = null;
            Element? afterTmp = null;
            Element? coverTmp = null;
            var range = new TimeRange(start, end - start);

            foreach (Element? item in scene.Children.GetMarshal().Value)
            {
                if (item != this && item.ZIndex == zindex)
                {
                    if (item.Start < start
                        && (beforeTmp == null || beforeTmp.Start <= item.Start))
                    {
                        beforeTmp = item;
                    }

                    if (item.Range.End > end
                        && (afterTmp == null || afterTmp.Range.End >= item.Range.End))
                    {
                        afterTmp = item;
                    }

                    if (range.Contains(item.Range))
                    {
                        coverTmp = item;
                    }
                }
            }
            return (beforeTmp, afterTmp, coverTmp);
        }

        return (null, null, null);
    }

    internal (Element? Before, Element? After, Element? Cover) GetBeforeAndAfterAndCover(int zindex, TimeSpan start, Element[] excludes)
    {
        if (HierarchicalParent is Scene scene)
        {
            Element? beforeTmp = null;
            Element? afterTmp = null;
            Element? coverTmp = null;
            var range = new TimeRange(start, Length);

            foreach (Element? item in scene.Children.Except(excludes))
            {
                if (item != this && item.ZIndex == zindex)
                {
                    if (item.Start < start
                        && (beforeTmp == null || beforeTmp.Start <= item.Start))
                    {
                        beforeTmp = item;
                    }

                    if (item.Range.End > range.End
                        && (afterTmp == null || afterTmp.Range.End >= item.Range.End))
                    {
                        afterTmp = item;
                    }

                    if (range.Contains(item.Range))
                    {
                        coverTmp = item;
                    }
                }
            }
            return (beforeTmp, afterTmp, coverTmp);
        }

        return (null, null, null);
    }
}