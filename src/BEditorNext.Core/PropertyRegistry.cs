﻿using System.Runtime.CompilerServices;

namespace BEditorNext;

public static class PropertyRegistry
{
    private static readonly Dictionary<int, CoreProperty> s_properties = new();
    private static readonly Dictionary<Type, Dictionary<int, CoreProperty>> s_registered = new();
    private static readonly Dictionary<Type, Dictionary<int, CoreProperty>> s_attached = new();
    private static readonly Dictionary<Type, List<CoreProperty>> s_registeredCache = new();
    private static readonly Dictionary<Type, List<CoreProperty>> s_attachedCache = new();

    public static IReadOnlyList<CoreProperty> GetRegistered(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (s_registeredCache.TryGetValue(type, out List<CoreProperty>? result))
        {
            return result;
        }

        Type? t = type;
        result = new List<CoreProperty>();

        while (t != null)
        {
            RuntimeHelpers.RunClassConstructor(t.TypeHandle);

            if (s_registered.TryGetValue(t, out Dictionary<int, CoreProperty>? registered))
            {
                result.AddRange(registered.Values);
            }

            t = t.BaseType;
        }

        s_registeredCache.Add(type, result);
        return result;
    }

    public static IReadOnlyList<CoreProperty> GetRegisteredAttached(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (s_attachedCache.TryGetValue(type, out List<CoreProperty>? result))
        {
            return result;
        }

        Type? t = type;
        result = new List<CoreProperty>();

        while (t != null)
        {
            if (s_attached.TryGetValue(t, out Dictionary<int, CoreProperty>? attached))
            {
                result.AddRange(attached.Values);
            }

            t = t.BaseType;
        }

        s_attachedCache.Add(type, result);
        return result;
    }

    public static CoreProperty? FindRegistered(Type type, string name)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(name);
        if (name.Contains('.'))
        {
            throw new InvalidOperationException("Attached properties not supported.");
        }

        IReadOnlyList<CoreProperty> registered = GetRegistered(type);
        int registeredCount = registered.Count;

        for (int i = 0; i < registeredCount; i++)
        {
            CoreProperty x = registered[i];

            if (x.Name == name)
            {
                return x;
            }
        }

        return null;
    }

    public static CoreProperty? FindRegistered(IElement o, string name)
    {
        ArgumentNullException.ThrowIfNull(o);
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
        }

        return FindRegistered(o.GetType(), name);
    }

    public static CoreProperty? FindRegistered(int id)
    {
        return id < s_properties.Count ? s_properties[id] : null;
    }

    public static bool IsRegistered(Type type, CoreProperty property)
    {
        static bool ContainsProperty(IReadOnlyList<CoreProperty> properties, CoreProperty property)
        {
            int propertiesCount = properties.Count;

            for (int i = 0; i < propertiesCount; i++)
            {
                if (properties[i] == property)
                {
                    return true;
                }
            }

            return false;
        }

        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(property);

        return ContainsProperty(GetRegistered(type), property) ||
               ContainsProperty(GetRegisteredAttached(type), property);
    }

    public static bool IsRegistered(object o, CoreProperty property)
    {
        ArgumentNullException.ThrowIfNull(o);
        ArgumentNullException.ThrowIfNull(property);

        return IsRegistered(o.GetType(), property);
    }

    public static void Register(Type type, CoreProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(type);

        if (!s_registered.TryGetValue(type, out Dictionary<int, CoreProperty>? inner))
        {
            inner = new Dictionary<int, CoreProperty>
            {
                { property.Id, property },
            };
            s_registered.Add(type, inner);
        }
        else if (!inner.ContainsKey(property.Id))
        {
            inner.Add(property.Id, property);
        }

        if (!s_properties.ContainsKey(property.Id))
        {
            s_properties.Add(property.Id, property);
        }

        s_registeredCache.Clear();
    }

    //public static void RegisterAttached(Type type, CoreProperty property)
    //{
    //    if (!property.IsAttached)
    //    {
    //        throw new InvalidOperationException("Cannot register a non-attached property as attached.");
    //    }

    //    if (!s_attached.TryGetValue(type, out Dictionary<int, CoreProperty>? inner))
    //    {
    //        inner = new Dictionary<int, CoreProperty>
    //        {
    //            { property.Id, property },
    //        };
    //        s_attached.Add(type, inner);
    //    }
    //    else
    //    {
    //        inner.Add(property.Id, property);
    //    }

    //    if (!s_properties.ContainsKey(property.Id))
    //    {
    //        s_properties.Add(property.Id, property);
    //    }

    //    s_attachedCache.Clear();
    //}
}
