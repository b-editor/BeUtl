﻿namespace BeUtl.Collections;

public sealed class LogicalList<T> : CoreList<T>
    where T : ILogicalElement
{
    public LogicalList(ILogicalElement parent)
    {
        Parent = parent;
        Attached = item => item.NotifyAttachedToLogicalTree(new LogicalTreeAttachmentEventArgs(Parent));
        Detached = item => item.NotifyDetachedFromLogicalTree(new LogicalTreeAttachmentEventArgs(null));
    }

    public ILogicalElement Parent { get; }
}
