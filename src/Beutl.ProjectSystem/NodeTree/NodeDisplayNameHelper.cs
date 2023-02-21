﻿using Beutl.NodeTree.Nodes.Group;

namespace Beutl.NodeTree;

internal class NodeDisplayNameHelper
{
    public static string GetDisplayName(INodeItem item)
    {
        string? name = (item as CoreObject)?.Name;
        if (string.IsNullOrWhiteSpace(name) || name == "Unknown")
        {
            name = null;
        }

        if (item.Property is { } property)
        {
            CorePropertyMetadata metadata = property.Property.GetMetadata<CorePropertyMetadata>(property.ImplementedType);

            return metadata.DisplayAttribute?.GetName() ?? name ?? property.Property.Name;
        }
        else if (item is IGroupSocket { AssociatedProperty: { } asProperty })
        {
            CorePropertyMetadata metadata = asProperty.GetMetadata<CorePropertyMetadata>(asProperty.OwnerType);

            return metadata.DisplayAttribute?.GetName() ?? name ?? asProperty.Name;
        }
        else
        {
            return name ?? "Unknown";
        }
    }

    public static string GetDisplayName(CoreProperty property)
    {
        CorePropertyMetadata metadata = property.GetMetadata<CorePropertyMetadata>(property.OwnerType);

        return metadata.DisplayAttribute?.GetName() ?? property.Name;
    }
}