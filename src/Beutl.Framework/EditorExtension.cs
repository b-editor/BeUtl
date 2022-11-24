﻿using System.Diagnostics.CodeAnalysis;

using Avalonia.Media;
using Avalonia.Platform.Storage;

using Beutl.Framework.Services;

using FluentAvalonia.UI.Controls;

using Microsoft.Extensions.DependencyInjection;

using Reactive.Bindings;

namespace Beutl.Framework;

public interface IEditorContext : IDisposable
{
    EditorExtension Extension { get; }

    string EdittingFile { get; }

    IReactiveProperty<bool> IsEnabled { get; }

    IKnownEditorCommands? Commands { get; }

    T? FindToolTab<T>(Func<T, bool> condition)
        where T : IToolContext;

    T? FindToolTab<T>()
        where T : IToolContext;

    bool OpenToolTab(IToolContext item);

    void CloseToolTab(IToolContext item);
}

// ファイルのエディタを追加
public abstract class EditorExtension : ViewExtension
{
    public abstract FilePickerFileType GetFilePickerFileType();

    public abstract IconSource? GetIcon();

    public abstract bool TryCreateEditor(
        string file,
        [NotNullWhen(true)] out IEditor? editor);

    // NOTE: ここからIWorkspaceItemを取得する場合、
    //       IWorkspaceItemContainerから取得すればいい
    public abstract bool TryCreateContext(
        string file,
        [NotNullWhen(true)] out IEditorContext? context);

    public virtual bool IsSupported(string file)
    {
        return MatchFileExtension(Path.GetExtension(file));
    }

    // extはピリオドを含む
    public abstract bool MatchFileExtension(string ext);

    // 'ServiceLocator'から'IProjectService'を取得し、Projectのインスタンスを取得します。
    protected static IWorkspace? GetCurrentProject()
    {
        return ServiceLocator.Current.GetRequiredService<IProjectService>().CurrentProject.Value;
    }
}
