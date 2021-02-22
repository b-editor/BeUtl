﻿
using System.Numerics;

namespace BEditor.Graphics
{
    public class OrthographicCamera : Camera
    {
        public OrthographicCamera(Vector3 position, float width, float height) : base(position)
        {
            Width = width;
            Height = height;
        }

        public float Width { get; set; }
        public float Height { get; set; }

        public override Matrix4x4 GetProjectionMatrix()
        {
            return Matrix4x4.CreateOrthographic(Width, Height, Near, Far);
        }
    }
}
