﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Beutl.NodeTree.Nodes.Group;

public interface IGroupSocket : ISocket
{
    CoreProperty? AssociatedProperty { get; set; }
}

public class GroupInput : Node, ISocketsCanBeAdded
{
    public SocketLocation PossibleLocation => SocketLocation.Right;

    public class GroupInputSocket<T> : OutputSocket<T>, IGroupSocket, IAutomaticallyGeneratedSocket
    {
        static GroupInputSocket()
        {
            NameProperty.OverrideMetadata<GroupInputSocket<T>>(new CorePropertyMetadata<string>("name"));
        }

        public CoreProperty? AssociatedProperty { get; set; }

        public override void ReadFromJson(JsonNode json)
        {
            base.ReadFromJson(json);
            JsonNode propertyJson = json["associated-property"]!;
            string name = (string)propertyJson["name"]!;
            string owner = (string)propertyJson["owner"]!;

            Type ownerType = TypeFormat.ToType(owner)!;

            AssociatedProperty = PropertyRegistry.GetRegistered(ownerType)
                .FirstOrDefault(x => x.GetMetadata<CorePropertyMetadata>(ownerType).SerializeName == name || x.Name == name);
        }

        public override void WriteToJson(ref JsonNode json)
        {
            base.WriteToJson(ref json);
            if (AssociatedProperty is { OwnerType: Type ownerType } property)
            {
                CorePropertyMetadata? metadata = property.GetMetadata<CorePropertyMetadata>(ownerType);
                string name = metadata.SerializeName ?? property.Name;
                string owner = TypeFormat.ToString(ownerType);

                json["associated-property"] = new JsonObject
                {
                    ["name"] = name,
                    ["owner"] = owner,
                };
            }
        }
    }

    public bool AddSocket(ISocket socket, [NotNullWhen(true)] out IConnection? connection)
    {
        connection = null;
        if (socket is IInputSocket { AssociatedType: { } valueType } inputSocket)
        {
            Type type = typeof(GroupInputSocket<>).MakeGenericType(valueType);

            if (Activator.CreateInstance(type) is IOutputSocket outputSocket)
            {
                ((NodeItem)outputSocket).LocalId = NextLocalId++;
                ((CoreObject)outputSocket).Name = NodeDisplayNameHelper.GetDisplayName(inputSocket);
                ((IGroupSocket)outputSocket).AssociatedProperty = inputSocket.Property?.Property;

                Items.Add(outputSocket);
                if (outputSocket.TryConnect(inputSocket))
                {
                    connection = inputSocket.Connection!;
                    return true;
                }
                else
                {
                    Items.Remove(outputSocket);
                }
            }
        }

        return false;
    }

    public override void ReadFromJson(JsonNode json)
    {
        base.ReadFromJson(json);
        if (json is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("items", out var itemsNode)
                && itemsNode is JsonArray itemsArray)
            {
                int index = 0;
                foreach (JsonObject itemJson in itemsArray.OfType<JsonObject>())
                {
                    if (itemJson.TryGetPropertyValue("@type", out JsonNode? atTypeNode)
                        && atTypeNode is JsonValue atTypeValue
                        && atTypeValue.TryGetValue(out string? atType))
                    {
                        var type = TypeFormat.ToType(atType);
                        IOutputSocket? socket = null;

                        if (type?.IsAssignableTo(typeof(IOutputSocket)) ?? false)
                        {
                            socket = Activator.CreateInstance(type) as IOutputSocket;
                        }

                        if (socket != null)
                        {
                            (socket as IJsonSerializable)?.ReadFromJson(itemJson);
                            Items.Add(socket);
                            ((NodeItem)socket).LocalId = index;
                        }
                    }

                    index++;
                }

                NextLocalId = index;
            }
        }
    }
}

public class GroupOutput : Node, ISocketsCanBeAdded
{
    public SocketLocation PossibleLocation => SocketLocation.Left;

    public class GroupOutputSocket<T> : InputSocket<T>, IAutomaticallyGeneratedSocket
    {
        static GroupOutputSocket()
        {
            NameProperty.OverrideMetadata<GroupOutputSocket<T>>(new CorePropertyMetadata<string>("name"));
        }
    }

    public bool AddSocket(ISocket socket, [NotNullWhen(true)] out IConnection? connection)
    {
        connection = null;
        if (socket is IOutputSocket { AssociatedType: { } valueType } outputSocket)
        {
            Type type = typeof(GroupOutputSocket<>).MakeGenericType(valueType);

            if (Activator.CreateInstance(type) is IInputSocket inputSocket)
            {
                ((NodeItem)inputSocket).LocalId = NextLocalId++;
                ((CoreObject)inputSocket).Name = NodeDisplayNameHelper.GetDisplayName(outputSocket);
                //((IGroupSocket)inputSocket).AssociatedProperty = outputSocket.Property?.Property;

                Items.Add(inputSocket);
                if (outputSocket.TryConnect(inputSocket))
                {
                    connection = inputSocket.Connection!;
                    return true;
                }
                else
                {
                    Items.Remove(outputSocket);
                }
            }
        }

        return false;
    }

    public override void ReadFromJson(JsonNode json)
    {
        base.ReadFromJson(json);
        if (json is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("items", out var itemsNode)
                && itemsNode is JsonArray itemsArray)
            {
                int index = 0;
                foreach (JsonObject itemJson in itemsArray.OfType<JsonObject>())
                {
                    if (itemJson.TryGetPropertyValue("@type", out JsonNode? atTypeNode)
                        && atTypeNode is JsonValue atTypeValue
                        && atTypeValue.TryGetValue(out string? atType))
                    {
                        var type = TypeFormat.ToType(atType);
                        IInputSocket? socket = null;

                        if (type?.IsAssignableTo(typeof(IInputSocket)) ?? false)
                        {
                            socket = Activator.CreateInstance(type) as IInputSocket;
                        }

                        if (socket != null)
                        {
                            (socket as IJsonSerializable)?.ReadFromJson(itemJson);
                            Items.Add(socket);
                            ((NodeItem)socket).LocalId = index;
                        }
                    }

                    index++;
                }

                NextLocalId = index;
            }
        }
    }
}