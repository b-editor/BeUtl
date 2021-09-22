﻿// Scene.cs
//
// Copyright (C) BEditor
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;

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
        /// <summary>
        /// Defines the <see cref="Start"/> property.
        /// </summary>
        public static readonly DirectProperty<SceneObject, EaseProperty> StartProperty = VideoFile.StartProperty.WithOwner<SceneObject>(
            owner => owner.Start,
            (owner, obj) => owner.Start = obj);

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneObject"/> class.
        /// </summary>
        public SceneObject()
        {
            // この時点で親要素を取得できないので適当なデータを渡す
            SelectScene = new(new SelectorPropertyMetadata(string.Empty, new string[1]));
        }

        /// <inheritdoc/>
        public override string Name => Strings.Scene;

        /// <summary>
        /// Gets the start position.
        /// </summary>
        [AllowNull]
        public EaseProperty Start { get; private set; }

        /// <summary>
        /// Gets the <see cref="SelectorProperty"/> to select the <seealso cref="Scene"/> to reference.
        /// </summary>
        [AllowNull]
        public SelectorProperty SelectScene { get; private set; }

        /// <inheritdoc/>
        public override IEnumerable<PropertyElement> GetProperties()
        {
            yield return Coordinate;
            yield return Scale;
            yield return Blend;
            yield return Rotate;
            yield return Material;
            yield return Start;
            yield return SelectScene;
        }

        /// <inheritdoc/>
        public override void SetObjectData(DeserializeContext context)
        {
            base.SetObjectData(context);
            SelectScene = (SelectorProperty)FormatterServices.GetUninitializedObject(typeof(SelectorProperty));
            SelectScene.SetObjectData(context.WithElement(context.Element.GetProperty(nameof(SelectScene))).WithParent(this));
        }

        /// <inheritdoc/>
        public override void GetObjectData(Utf8JsonWriter writer)
        {
            base.GetObjectData(writer);
            writer.WriteStartObject(nameof(SelectScene));
            SelectScene.GetObjectData(writer);
            writer.WriteEndObject();
        }

        /// <inheritdoc/>
        protected override Image<BGRA32>? OnRender(EffectApplyArgs args)
        {
            var scene = this.GetParent<Project>()?.Children.First(i => i.Name == SelectScene.SelectItem!) ?? Parent!.Parent;
            if (scene.Equals(this.GetParent<Scene>())) return null;

            // Clipの相対的なフレーム
            var frame = args.Frame - Parent!.Start;

            var img = scene.Render(frame + (int)Start[args.Frame], ApplyType.Image);
            Parent.Parent.GraphicsContext!.PlatformImpl.MakeCurrent();

            return img;
        }

        /// <inheritdoc/>
        protected override void OnLoad()
        {
            base.OnLoad();
            SelectScene.PropertyMetadata = new ScenesSelectorMetadata(this);
            SelectScene.Load();
        }

        /// <inheritdoc/>
        protected override void OnUnload()
        {
            base.OnUnload();
            SelectScene.Unload();
        }

        internal record ScenesSelectorMetadata : SelectorPropertyMetadata
        {
            internal ScenesSelectorMetadata(SceneObject scene)
                : base(Strings.Scenes, scene.GetParent<Project>()!.SceneList.Select(i => i.Name).ToArray())
            {
            }
        }
    }
}