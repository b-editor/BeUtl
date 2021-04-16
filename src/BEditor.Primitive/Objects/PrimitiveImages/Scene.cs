﻿using System;
using System.Collections.Generic;
using System.Linq;

using BEditor.Data;
using BEditor.Data.Primitive;
using BEditor.Data.Property;
using BEditor.Drawing;
using BEditor.Drawing.Pixel;
using BEditor.Primitive.Resources;

namespace BEditor.Primitive.Objects
{
    /// <summary>
    /// Represents an <see cref="ImageObject"/> that refers to a <see cref="Scene"/>.
    /// </summary>
    public sealed class SceneObject : ImageObject
    {
        SelectorPropertyMetadata? SelectSceneMetadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneObject"/> class.
        /// </summary>
        public SceneObject()
        {
            Start = new(VideoFile.StartMetadata);

            // この時点で親要素を取得できないので適当なデータを渡す
            SelectScene = new(new SelectorPropertyMetadata("", new string[1]));
        }

        /// <inheritdoc/>
        public override string Name => Strings.Scene;
        /// <inheritdoc/>
        public override IEnumerable<PropertyElement> Properties => new PropertyElement[]
        {
            Coordinate,
            Scale,
            Blend,
            Rotate,
            Material,
            Start,
            SelectScene
        };
        /// <summary>
        /// Get the <see cref="EaseProperty"/> that represents the start position.
        /// </summary>
        [DataMember]
        public EaseProperty Start { get; private set; }
        /// <summary>
        /// Get the <see cref="SelectorProperty"/> to select the <seealso cref="Scene"/> to reference.
        /// </summary>
        [DataMember]
        public SelectorProperty SelectScene { get; private set; }

        /// <inheritdoc/>
        protected override Image<BGRA32>? OnRender(EffectRenderArgs args)
        {
            var scene = this.GetParent3()?.Find(SelectScene.SelectItem!) ?? Parent!.Parent;
            if (scene.Equals(this.GetParent2())) return null;

            // Clipの相対的なフレーム
            var frame = args.Frame - Parent!.Start;

            var img = scene.Render(frame + (int)Start[args.Frame], RenderType.ImageOutput);
            Parent.Parent.GraphicsContext!.MakeCurrent();
            Parent.Parent.GraphicsContext.Framebuffer.Bind();

            return img;
        }
        /// <inheritdoc/>
        protected override void OnLoad()
        {
            base.OnLoad();
            SelectSceneMetadata = new ScenesSelectorMetadata(this);
            Start.Load(VideoFile.StartMetadata);
            SelectScene.Load(SelectSceneMetadata);
        }
        /// <inheritdoc/>
        protected override void OnUnload()
        {
            base.OnUnload();
            Start.Unload();
            SelectScene.Unload();
        }

        internal record ScenesSelectorMetadata : SelectorPropertyMetadata
        {
            internal ScenesSelectorMetadata(SceneObject scene) : base(Strings.Scenes, scene.GetParent3()!.SceneList.Select(i => i.SceneName).ToArray())
            {
            }
        }
    }
}