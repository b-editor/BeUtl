﻿using System.Diagnostics.CodeAnalysis;

using Avalonia.Media;

using BeUtl.Controls;
using BeUtl.Framework;
using BeUtl.ProjectSystem;
using BeUtl.ViewModels;
using BeUtl.Views;

namespace BeUtl.Services;

public sealed class SceneEditorExtension : EditorExtension
{
    public static readonly SceneEditorExtension Instance = new();

    public override Geometry? Icon { get; } = FluentIconsRegular.Document.GetGeometry();

    public override string[] FileExtensions { get; } =
    {
        "scene"
    };

    public override ResourceReference<string> FileTypeName => "S.Common.SceneFile";

    public override string Name => "Scene editor";

    public override string DisplayName => "Scene editor";

    public override bool TryCreateEditor(string file, [NotNullWhen(true)] out IEditor? editor)
    {
        if (file.EndsWith(".scene"))
        {
            editor = new EditView();
            return true;
        }
        else
        {
            editor = null;
            return false;
        }
    }

    public override bool TryCreateContext(string file, [NotNullWhen(true)] out IEditorContext? context)
    {
        if (file.EndsWith(".scene"))
        {
            context = new EditViewModel(GetOrCreateScene(file));
            return true;
        }
        else
        {
            context = null;
            return false;
        }
    }

    private static Scene GetOrCreateScene(string file)
    {
        Project? proj = GetCurrentProject();
        if (proj != null)
        {
            foreach (Scene scn in proj.Children.AsSpan())
            {
                if (scn.FileName == file)
                {
                    return scn;
                }
            }
        }

        var scn1 = new Scene();
        scn1.Restore(file);

        if (proj != null)
        {
            proj.Children.Add(scn1);
        }

        return scn1;
    }
}
