﻿// Text.cs
//
// Copyright (C) BEditor
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

using BEditor.Data;
using BEditor.Data.Primitive;
using BEditor.Data.Property;
using BEditor.Drawing;
using BEditor.Drawing.Pixel;
using BEditor.Graphics;
using BEditor.Media;
using BEditor.Primitive.Resources;

using SkiaSharp;

namespace BEditor.Primitive.Objects
{
    /// <summary>
    /// Represents an <see cref="ImageObject"/> to draw a string.
    /// </summary>
    public sealed class Text : ImageObject
    {
        /// <summary>
        /// Defines the <see cref="Size"/> property.
        /// </summary>
        public static readonly DirectProperty<Text, EaseProperty> SizeProperty = EditingProperty.RegisterDirect<EaseProperty, Text>(
            nameof(Size),
            owner => owner.Size,
            (owner, obj) => owner.Size = obj,
            EditingPropertyOptions<EaseProperty>.Create(new EasePropertyMetadata(Strings.Size, 100, float.NaN, 0)).Serialize());

        /// <summary>
        /// Defines the <see cref="LineSpacing"/> property.
        /// </summary>
        public static readonly DirectProperty<Text, EaseProperty> LineSpacingProperty = EditingProperty.RegisterDirect<EaseProperty, Text>(
            nameof(LineSpacing),
            owner => owner.LineSpacing,
            (owner, obj) => owner.LineSpacing = obj,
            EditingPropertyOptions<EaseProperty>.Create(new EasePropertyMetadata(Strings.LineSpacing, 0, float.NaN, 0)).Serialize());

        /// <summary>
        /// Defines the <see cref="Color"/> property.
        /// </summary>
        public static readonly DirectProperty<Text, ColorProperty> ColorProperty = EditingProperty.RegisterDirect<ColorProperty, Text>(
            nameof(Color),
            owner => owner.Color,
            (owner, obj) => owner.Color = obj,
            EditingPropertyOptions<ColorProperty>.Create(new ColorPropertyMetadata(Strings.Color, Colors.White)).Serialize());

        /// <summary>
        /// Defines the <see cref="Color"/> property.
        /// </summary>
        public static readonly DirectProperty<Text, FontProperty> FontProperty = EditingProperty.RegisterDirect<FontProperty, Text>(
            nameof(Font),
            owner => owner.Font,
            (owner, obj) => owner.Font = obj,
            EditingPropertyOptions<FontProperty>.Create(new FontPropertyMetadata()).Serialize());

        /// <summary>
        /// Defines the <see cref="HorizontalAlign"/> property.
        /// </summary>
        public static readonly DirectProperty<Text, SelectorProperty> HorizontalAlignProperty = EditingProperty.RegisterDirect<SelectorProperty, Text>(
            nameof(HorizontalAlign),
            owner => owner.HorizontalAlign,
            (owner, obj) => owner.HorizontalAlign = obj,
            EditingPropertyOptions<SelectorProperty>.Create(new SelectorPropertyMetadata(Strings.HorizontalAlignment, new[]
            {
                Strings.Left,
                Strings.Center,
                Strings.Right,
            })).Serialize());

        /// <summary>
        /// Defines the <see cref="VerticalAlign"/> property.
        /// </summary>
        public static readonly DirectProperty<Text, SelectorProperty> VerticalAlignProperty = EditingProperty.RegisterDirect<SelectorProperty, Text>(
            nameof(VerticalAlign),
            owner => owner.VerticalAlign,
            (owner, obj) => owner.VerticalAlign = obj,
            EditingPropertyOptions<SelectorProperty>.Create(new SelectorPropertyMetadata(Strings.VerticalAlignment, new[]
            {
                Strings.Top,
                Strings.Center,
                Strings.Bottom,
            })).Serialize());

        /// <summary>
        /// Defines the <see cref="Document"/> property.
        /// </summary>
        public static readonly DirectProperty<Text, DocumentProperty> DocumentProperty = EditingProperty.RegisterDirect<DocumentProperty, Text>(
            nameof(Document),
            owner => owner.Document,
            (owner, obj) => owner.Document = obj,
            EditingPropertyOptions<DocumentProperty>.Create(new DocumentPropertyMetadata(string.Empty)).Serialize());

        /// <summary>
        /// Defines the <see cref="IsMultiple"/> property.
        /// </summary>
        public static readonly DirectProperty<Text, CheckProperty> IsMultipleProperty = EditingProperty.RegisterDirect<CheckProperty, Text>(
            nameof(IsMultiple),
            owner => owner.IsMultiple,
            (owner, obj) => owner.IsMultiple = obj,
            EditingPropertyOptions<CheckProperty>.Create(new CheckPropertyMetadata(string.Empty, true)).Serialize());

        /// <summary>
        /// Initializes a new instance of the <see cref="Text"/> class.
        /// </summary>
        public Text()
        {
        }

        /// <inheritdoc/>
        public override string Name => Strings.Text;

        /// <summary>
        /// Gets the size of the string to be drawn.
        /// </summary>
        [AllowNull]
        public EaseProperty Size { get; private set; }

        /// <summary>
        /// Gets the line spacing of the string to be drawn.
        /// </summary>
        [AllowNull]
        public EaseProperty LineSpacing { get; private set; }

        /// <summary>
        /// Gets the color of string to be drawn.
        /// </summary>
        [AllowNull]
        public ColorProperty Color { get; private set; }

        /// <summary>
        /// Gets the font of the string to be drawn.
        /// </summary>
        [AllowNull]
        public FontProperty Font { get; private set; }

        /// <summary>
        /// Gets the horizontal alignment of the string to be drawn.
        /// </summary>
        [AllowNull]
        public SelectorProperty HorizontalAlign { get; private set; }

        /// <summary>
        /// Gets the vertical alignment of the string to be drawn.
        /// </summary>
        [AllowNull]
        public SelectorProperty VerticalAlign { get; private set; }

        /// <summary>
        /// Gets the string to be drawn.
        /// </summary>
        [AllowNull]
        public DocumentProperty Document { get; private set; }

        /// <summary>
        /// Gets the string to be drawn.
        /// </summary>
        [AllowNull]
        public CheckProperty IsMultiple { get; private set; }

        /// <inheritdoc/>
        public override IEnumerable<PropertyElement> GetProperties()
        {
            yield return Coordinate;
            yield return Scale;
            yield return Blend;
            yield return Rotate;
            yield return Material;
            yield return Size;
            yield return LineSpacing;
            yield return Color;
            yield return Font;
            yield return HorizontalAlign;
            yield return VerticalAlign;
            yield return Document;
            yield return IsMultiple;
        }

        /// <inheritdoc/>
        protected override Image<BGRA32> OnRender(EffectApplyArgs args)
        {
            var fmtText = new FormattedText(
                Document.Value,
                Font.Value,
                Size[args.Frame],
                (TextAlignment)HorizontalAlign.Index,
                new FormattedTextStyleSpan[] { new(0, Document.Value.Length, Color.Value), });

            return fmtText.Draw();
        }

        /// <inheritdoc/>
        protected override void OnRender(EffectApplyArgs<IEnumerable<ImageInfo>> args)
        {
            if (IsMultiple.Value)
            {
                args.Value = Selector(args.Frame);
            }
            else
            {
                base.OnRender(args);
            }
        }

        private IEnumerable<ImageInfo> Selector(Frame frame)
        {
            var fmtText = new FormattedText(
                Document.Value,
                Font.Value,
                Size[frame],
                (TextAlignment)HorizontalAlign.Index,
                new FormattedTextStyleSpan[] { new(0, Document.Value.Length, Color.Value), });
            var bounds = fmtText.Bounds;

            foreach (var (image, rect) in fmtText.DrawMultiple())
            {
                yield return new ImageInfo(image, _ =>
                {
                    var x = rect.X + (rect.Width / 2) - (bounds.Width / 2);
                    var y = rect.Y + (rect.Height / 2) - (bounds.Height / 2);
                    return new Transform(new(x, -y, 0), default, default, default);
                });
            }
        }
    }
}