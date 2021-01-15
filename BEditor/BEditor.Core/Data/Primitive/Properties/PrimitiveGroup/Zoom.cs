﻿using System.Collections.Generic;
using System.Runtime.Serialization;

using BEditor.Core.Command;
using BEditor.Core.Data.Property;
using BEditor.Core.Extensions;
using BEditor.Core.Properties;

namespace BEditor.Core.Data.Primitive.Properties.PrimitiveGroup
{
    [DataContract]
    public sealed class Zoom : ExpandGroup
    {
        public static readonly EasePropertyMetadata ZoomMetadata = new(Resources.Zoom, 100);
        public static readonly EasePropertyMetadata ScaleXMetadata = new(Resources.X, 100);
        public static readonly EasePropertyMetadata ScaleYMetadata = new(Resources.Y, 100);
        public static readonly EasePropertyMetadata ScaleZMetadata = new(Resources.Z, 100);

        public Zoom(PropertyElementMetadata metadata) : base(metadata)
        {
            Scale = new(ZoomMetadata);
            ScaleX = new(ScaleXMetadata);
            ScaleY = new(ScaleYMetadata);
            ScaleZ = new(ScaleZMetadata);
        }

        public override IEnumerable<PropertyElement> Properties => new PropertyElement[]
        {
            Scale,
            ScaleX,
            ScaleY,
            ScaleZ
        };
        [DataMember(Name = "Zoom", Order = 0)]
        public EaseProperty Scale { get; private set; }
        [DataMember(Order = 1)]
        public EaseProperty ScaleX { get; private set; }
        [DataMember(Order = 2)]
        public EaseProperty ScaleY { get; private set; }
        [DataMember(Order = 3)]
        public EaseProperty ScaleZ { get; private set; }

        public override void Loaded()
        {
            if (IsLoaded) return;

            Scale.ExecuteLoaded(ZoomMetadata);
            ScaleX.ExecuteLoaded(ScaleXMetadata);
            ScaleY.ExecuteLoaded(ScaleYMetadata);
            ScaleZ.ExecuteLoaded(ScaleZMetadata);

            base.Loaded();
        }
        public override void Unloaded()
        {
            if (!IsLoaded) return;
            
            Scale.Unloaded();
            ScaleX.Unloaded();
            ScaleY.Unloaded();
            ScaleZ.Unloaded();

            base.Unloaded();
        }
    }
}
