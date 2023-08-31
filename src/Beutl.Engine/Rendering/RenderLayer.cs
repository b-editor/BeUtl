﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Beutl.Audio;
using Beutl.Graphics;
using Beutl.Graphics.Rendering;
using Beutl.Media;
using Beutl.Rendering.Cache;

namespace Beutl.Rendering;

public sealed class RenderLayer : IDisposable
{
    private class Entry : IDisposable
    {
        public Entry(DrawableNode node)
        {
            Node = node;
            IsDirty = true;
        }

        ~Entry()
        {
            Dispose();
        }

        public DrawableNode Node { get; }

        public bool IsDirty { get; set; }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Node.Dispose();
                IsDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }

    // このテーブルは本描画するときに、自分のレイヤー以外のものを削除する。
    private readonly ConditionalWeakTable<Drawable, Entry> _cache = new();
    private readonly RenderScene _renderScene;
    private List<Entry>? _currentFrame;

    public RenderLayer(RenderScene renderScene)
    {
        _renderScene = renderScene;
    }

    private List<Entry> CurrentFrame => _currentFrame ??= new(1);

    public void Clear()
    {
        _currentFrame?.Clear();
    }

    public void UpdateAll(IReadOnlyList<Drawable> elements)
    {
        _currentFrame?.Clear();
        if (elements.Count == 0)
        {
            return;
        }

        CurrentFrame.EnsureCapacity(elements.Count);

        foreach (Drawable drawable in elements)
        {
            if (!_cache.TryGetValue(drawable, out Entry? entry))
            {
                entry = new Entry(new DrawableNode(drawable));
                _cache.Add(drawable, entry);

                var weakRef = new WeakReference<Entry>(entry);
                EventHandler<RenderInvalidatedEventArgs>? handler = null;
                handler = (_, _) =>
                {
                    if (weakRef.TryGetTarget(out Entry? obj))
                    {
                        obj.IsDirty = true;
                    }
                    else
                    {
                        drawable.Invalidated -= handler;
                    }
                };
                drawable.Invalidated += handler;
            }

            if (entry.IsDirty)
            {
                // DeferredCanvasを作成し、記録
                var canvas = new DeferradCanvas(entry.Node, _renderScene.Size);
                drawable.Render(canvas);
                entry.IsDirty = false;
            }

            CurrentFrame.Add(entry);
        }
    }

    public void ClearAllNodeCache(RenderCacheContext? context)
    {
        foreach (KeyValuePair<Drawable, Entry> item in _cache)
        {
            context?.ClearCache(item.Value.Node);

            item.Value.Dispose();
        }

        _cache.Clear();
    }

    public void Render(ImmediateCanvas canvas)
    {
        foreach (Entry? entry in CollectionsMarshal.AsSpan(_currentFrame))
        {
            DrawableNode node = entry.Node;
            Drawable drawable = node.Drawable;
            if (entry.IsDirty)
            {
                var dcanvas = new DeferradCanvas(node, _renderScene.Size);
                drawable.Render(dcanvas);
                entry.IsDirty = false;
            }

            canvas.DrawNode(node);

            canvas.GetCacheContext()?.MakeCache(node, canvas);
        }
    }

    public void Dispose()
    {
        foreach (KeyValuePair<Drawable, Entry> item in _cache)
        {
            item.Value.Dispose();
        }

        _cache.Clear();
        _currentFrame?.Clear();
    }
}
