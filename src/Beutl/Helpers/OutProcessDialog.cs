﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using DynamicData;

namespace Beutl.Helpers;

public static class OutProcessDialog
{
    public static IDisposable Show(
        string? title,
        string? subtitle,
        string? content,
        string? icon,
        bool progress = false,
        bool closable = false)
    {
        var startInfo = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "Beutl.WaitingDialog"));
        startInfo.ArgumentList.AddRange(["--parent", Environment.ProcessId.ToString()]);

        if (!string.IsNullOrWhiteSpace(title))
            startInfo.ArgumentList.AddRange(["--title", title]);

        if (!string.IsNullOrWhiteSpace(subtitle))
            startInfo.ArgumentList.AddRange(["--subtitle", subtitle]);

        if (!string.IsNullOrWhiteSpace(content))
            startInfo.ArgumentList.AddRange(["--content", content]);

        if (!string.IsNullOrWhiteSpace(icon))
            startInfo.ArgumentList.AddRange(["--icon", icon]);

        if (progress)
            startInfo.ArgumentList.Add("--progress");

        if (closable)
            startInfo.ArgumentList.Add("--closable");

        var process = Process.Start(startInfo);

        void ProcessExit()
        {
            process?.Kill();
            process?.Dispose();
            process = null;
        }

        return Disposable.Create(ProcessExit);
    }
}
