﻿using System.Collections.Generic;

using BEditor.Data;
using BEditor.Data.Primitive;
using BEditor.Data.Property;
using BEditor.Drawing;
using BEditor.Drawing.Pixel;
using BEditor.Properties;

namespace BEditor.Primitive.Effects
{
    /// <summary>
    /// Represents an <see cref="ImageEffect"/> that monochromatizes an image.
    /// </summary>
    public sealed class Monoc : ImageEffect
    {
        /// <summary>
        /// Represents <see cref="Color"/> metadata.
        /// </summary>
        public static readonly ColorPropertyMetadata ColorMetadata = new(Resources.Color, Drawing.Color.Light);

        /// <summary>
        /// Initializes a new instance of the <see cref="Monoc"/> class.
        /// </summary>
        public Monoc()
        {
            Color = new(ColorMetadata);
        }

        /// <inheritdoc/>
        public override string Name => Resources.Monoc;
        /// <inheritdoc/>
        public override IEnumerable<PropertyElement> Properties => new PropertyElement[]
        {
            Color
        };
        /// <summary>
        /// Get the <see cref="ColorProperty"/> that represents the color to be monochromatic.
        /// </summary>
        [DataMember]
        public ColorProperty Color { get; private set; }

        /// <inheritdoc/>
        public override void Render(EffectRenderArgs<Image<BGRA32>> args)
        {
            args.Value.SetColor(Color.Value);
        }
        /// <inheritdoc/>
        protected override void OnLoad()
        {
            Color.Load(ColorMetadata);
        }
        /// <inheritdoc/>
        protected override void OnUnload()
        {
            foreach (var pr in Children)
            {
                pr.Unload();
            }
        }
    }
}
