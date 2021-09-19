﻿// ClipElement.Methods.cs
//
// Copyright (C) BEditor
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;

using BEditor.Command;
using BEditor.Media;

namespace BEditor.Data
{
    /// <summary>
    /// Represents a data of a clip to be placed in the timeline.
    /// </summary>
    public sealed partial class ClipElement : ICloneable, IJsonObject, IElementObject
    {
        /// <inheritdoc cref="ICloneable.Clone"/>
        public ClipElement Clone()
        {
            var clip = this.DeepClone();

            clip!.Parent = Parent;
            clip.Load();

            return clip;
        }

        /// <inheritdoc/>
        public override void GetObjectData(Utf8JsonWriter writer)
        {
            base.GetObjectData(writer);
            writer.WriteNumber(nameof(Start), Start);
            writer.WriteNumber(nameof(End), End);
            writer.WriteNumber(nameof(Layer), Layer);

            writer.WriteStartArray("Effects");
            foreach (var effect in Effect)
            {
                writer.WriteStartObject();

                var type = effect.GetType();
                writer.WriteString("_type", type.FullName + ", " + type.Assembly.GetName().Name);
                effect.GetObjectData(writer);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        /// <inheritdoc/>
        public override void SetObjectData(DeserializeContext context)
        {
            base.SetObjectData(context);
            var element = context.Element;
            Parent = (context.Parent as Scene) ?? Parent;

            Start = element.GetProperty(nameof(Start)).GetInt32();
            End = element.GetProperty(nameof(End)).GetInt32();
            Layer = element.GetProperty(nameof(Layer)).GetInt32();
            var effects = element.GetProperty("Effects");
            _effect = new();
            foreach (var effect in effects.EnumerateArray())
            {
                var typeName = effect.GetProperty("_type").GetString() ?? string.Empty;
                if (Type.GetType(typeName) is var type && type is not null)
                {
                    var obj = (EffectElement)FormatterServices.GetUninitializedObject(type);
                    obj.SetObjectData(context.WithParent(this).WithElement(effect));

                    Effect.Add(obj);
                }
            }

            Metadata = ObjectMetadata.LoadedObjects.First(i => i.Type == Effect[0].GetType());

            // Todo: 互換性
            Name = element.TryGetProperty("Text", out var txt) ? txt.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Create a command to add an <see cref="EffectElement"/> to this <see cref="ClipElement"/>.
        /// </summary>
        /// <param name="effect">The <see cref="EffectElement"/> to add.</param>
        /// <returns>Created <see cref="IRecordCommand"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="effect"/> is <see langword="null"/>.</exception>
        [Pure]
        public IRecordCommand AddEffect(EffectElement effect)
        {
            if (effect is null) throw new ArgumentNullException(nameof(effect));

            return new EffectElement.AddCommand(effect, this);
        }

        /// <summary>
        /// Create a command to remove an <see cref="EffectElement"/> to this <see cref="ClipElement"/>.
        /// </summary>
        /// <param name="effect">The <see cref="EffectElement"/> to remove.</param>
        /// <returns>Created <see cref="IRecordCommand"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="effect"/> is <see langword="null"/>.</exception>
        [Pure]
        public IRecordCommand RemoveEffect(EffectElement effect)
        {
            if (effect is null) throw new ArgumentNullException(nameof(effect));

            return new EffectElement.RemoveCommand(effect, this);
        }

        /// <summary>
        /// Create a command to move this <see cref="ClipElement"/> frames and layers.
        /// </summary>
        /// <param name="newframe">The new starting frame of this <see cref="ClipElement"/>.</param>
        /// <param name="newlayer">The new layer of this <see cref="ClipElement"/>.</param>
        /// <returns>Created <see cref="IRecordCommand"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="newframe"/> or <paramref name="newlayer"/> is less than 0.</exception>
        [Pure]
        public IRecordCommand MoveFrameLayer(Frame newframe, int newlayer)
        {
            return new MoveCommand(this, newframe, newlayer);
        }

        /// <summary>
        /// Create a command to change the length of this <see cref="ClipElement"/>.
        /// </summary>
        /// <param name="start">The new starting frame of this <see cref="ClipElement"/>.</param>
        /// <param name="end">The new ending frame of this <see cref="ClipElement"/>.</param>
        /// <returns>Created <see cref="IRecordCommand"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> or <paramref name="end"/> is less than 0.</exception>
        [Obsolete("Use ChangeLength(ClipLengthChangeAnchor, Frame).", error: true)]
        public IRecordCommand ChangeLength(Frame start, Frame end)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Create a command to change the length of this <see cref="ClipElement"/>.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <param name="length">The new length of this <see cref="ClipElement"/>.</param>
        /// <returns>Created <see cref="IRecordCommand"/>.</returns>
        [Pure]
        public IRecordCommand ChangeLength(ClipLengthChangeAnchor anchor, Frame length)
        {
            return new LengthChangeCommand(this, length, anchor);
        }

        /// <summary>
        /// Create a command to split this clip at the specified frame.
        /// </summary>
        /// <param name="frame">The frame to be split.</param>
        /// <returns>Created <see cref="IRecordCommand"/>.</returns>
        [Pure]
        public IRecordCommand Split(Frame frame)
        {
            return new SplitCommand(this, frame);
        }

        /// <summary>
        /// Apply this <see cref="ClipElement"/>.
        /// </summary>
        /// <param name="args">The data used to apply the clip.</param>
        /// <exception cref="RenderingException">Faileds to rendering.</exception>
        public void Apply(ClipApplyArgs args)
        {
            try
            {
                var loadargs = new EffectApplyArgs(args.Frame, args.Contexts.Graphics, args.Contexts.Sampling, args.Contexts.Drawing, args.Type);

                if (Effect[0] is ObjectElement obj)
                {
                    if (!obj.IsEnabled) return;

                    obj.Apply(loadargs);
                }
            }
            catch (Exception e)
            {
                throw new RenderingException("Faileds to rendering.", e);
            }
        }

        /// <summary>
        /// Prepare this <see cref="ClipElement"/> for applying.
        /// </summary>
        /// <param name="args">The data used to applying the clip.</param>
        /// <exception cref="RenderingException">Faileds to rendering.</exception>
        public void PreviewApply(ClipApplyArgs args)
        {
            try
            {
                var enableEffects = Effect.Where(x => x.IsEnabled);
                var loadargs = new EffectApplyArgs(args.Frame, args.Contexts.Graphics, args.Contexts.Sampling, args.Contexts.Drawing, args.Type);

                foreach (var item in enableEffects)
                {
                    item.PreviewApply(loadargs);
                }
            }
            catch (Exception e)
            {
                throw new RenderingException("Faileds to rendering.", e);
            }
        }

        /// <inheritdoc/>
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// 指定した開始フレームにクリップを移動します.
        /// </summary>
        /// <param name="start">開始フレームです.</param>
        internal void MoveTo(Frame start)
        {
            var length = Length;
            Start = start;
            End = length + start;
        }
    }
}