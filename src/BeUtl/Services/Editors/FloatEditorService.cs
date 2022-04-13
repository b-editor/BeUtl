﻿using BeUtl.ProjectSystem;

namespace BeUtl.Services.Editors;

public sealed class FloatEditorService : INumberEditorService<float>
{
    public float GetMaximum(PropertyInstance<float> property)
    {
        return property.GetMaximumOrDefault(float.MaxValue);
    }

    public float GetMinimum(PropertyInstance<float> property)
    {
        return property.GetMinimumOrDefault(float.MinValue);
    }

    public float Clamp(float value, float min, float max)
    {
        return Math.Clamp(value, min, max);
    }

    public float Decrement(float value, int increment)
    {
        return value - increment;
    }

    public float Increment(float value, int increment)
    {
        return value + increment;
    }

    public bool TryParse(string? s, out float result)
    {
        return float.TryParse(s, out result);
    }
}
