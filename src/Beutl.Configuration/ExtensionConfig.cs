﻿using System.Text.Json.Nodes;

using Beutl.Collections;

namespace Beutl.Configuration;

public sealed class ExtensionConfig : ConfigurationBase
{
    public ExtensionConfig()
    {
        EditorExtensions.CollectionChanged += (_, _) => OnChanged();
        DecoderPriority.CollectionChanged += (_, _) => OnChanged();
    }

    public struct TypeLazy
    {
        private Type? _type = null;

        public TypeLazy(string formattedTypeName)
        {
            FormattedTypeName = formattedTypeName;
        }

        public string FormattedTypeName { get; init; }

        public Type? Type => _type ??= TypeFormat.ToType(FormattedTypeName);
    }

    // Keyには拡張子を含める
    public CoreDictionary<string, ICoreList<TypeLazy>> EditorExtensions { get; } = new();

    // Keyには拡張子を含める
    public CoreList<TypeLazy> DecoderPriority { get; } = new();

    public override void ReadFromJson(JsonObject json)
    {
        base.ReadFromJson(json);
        if (json.TryGetPropertyValue("editor-extensions", out JsonNode? eeNode)
            && eeNode is JsonObject eeObject)
        {
            EditorExtensions.Clear();
            foreach (KeyValuePair<string, JsonNode?> item in eeObject)
            {
                if (item.Value is JsonArray jsonArray)
                {
                    EditorExtensions.Add(item.Key, new CoreList<TypeLazy>(jsonArray.OfType<JsonValue>()
                        .Select(value => value.TryGetValue(out string? type) ? type : null)
                        .Select(str => new TypeLazy(str!))
                        .Where(type => type.FormattedTypeName != null)!));
                }
            }
        }

        if (json["decoder-priority"] is JsonArray dpArray)
        {
            DecoderPriority.Clear();
            DecoderPriority.AddRange(dpArray
                .Select(v => v?.AsValue()?.GetValue<string?>())
                .Where(v => v != null)
                .Select(v => new TypeLazy(v!)));
        }
    }

    public override void WriteToJson(JsonObject json)
    {
        base.WriteToJson(json);

        var eeObject = new JsonObject();
        foreach ((string key, ICoreList<TypeLazy> value) in EditorExtensions)
        {
            eeObject.Add(key, new JsonArray(value
                .Select(type => type.FormattedTypeName)
                .Select(str => JsonValue.Create(str))
                .ToArray()));
        }

        var dpArray = new JsonArray(DecoderPriority.Select(v => JsonValue.Create(v.FormattedTypeName)).ToArray());

        json["editor-extensions"] = eeObject;
        json["decoder-priority"] = dpArray;
    }
}
