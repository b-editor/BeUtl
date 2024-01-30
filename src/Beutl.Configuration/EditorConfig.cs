﻿using System.ComponentModel;
using System.Diagnostics;

using Beutl.Collections;
using Beutl.Serialization;

namespace Beutl.Configuration;

public enum LibraryTabDisplayMode
{
    Show,
    Hide
}

public enum FrameCacheConfigScale
{
    Original,

    FitToPreviewer,

    Half,

    Quarter,
}

public enum FrameCacheConfigColorType
{
    RGBA,

    YUV
}

public sealed class EditorConfig : ConfigurationBase
{
    public static readonly CoreProperty<bool> AutoAdjustSceneDurationProperty;
    public static readonly CoreProperty<bool> EnablePointerLockInPropertyProperty;
    public static readonly CoreProperty<bool> IsAutoSaveEnabledProperty;
    public static readonly CoreProperty<double> FrameCacheMaxSizeProperty;
    public static readonly CoreProperty<FrameCacheConfigScale> FrameCacheScaleProperty;
    public static readonly CoreProperty<FrameCacheConfigColorType> FrameCacheColorTypeProperty;

    static EditorConfig()
    {
        AutoAdjustSceneDurationProperty = ConfigureProperty<bool, EditorConfig>(nameof(AutoAdjustSceneDuration))
            .DefaultValue(true)
            .Register();

        EnablePointerLockInPropertyProperty = ConfigureProperty<bool, EditorConfig>(nameof(EnablePointerLockInProperty))
            .DefaultValue(true)
            .Register();

        IsAutoSaveEnabledProperty = ConfigureProperty<bool, EditorConfig>(nameof(IsAutoSaveEnabled))
            .DefaultValue(true)
            .Register();

        ulong memSize = OperatingSystem.IsWindows() ? GetWindowsMemoryCapacity()
            : OperatingSystem.IsLinux() ? GetLinuxMemoryCapacity()
            : 1024 * 1024 * 1024;
        double memSizeInMG = memSize / (1024d * 1024d);

        // デフォルトはメモリ容量の半分にする
        FrameCacheMaxSizeProperty = ConfigureProperty<double, EditorConfig>(nameof(FrameCacheMaxSize))
            .DefaultValue(memSizeInMG / 2)
            .Register();

        FrameCacheScaleProperty = ConfigureProperty<FrameCacheConfigScale, EditorConfig>(nameof(FrameCacheScale))
            .DefaultValue(FrameCacheConfigScale.FitToPreviewer)
            .Register();

        FrameCacheColorTypeProperty = ConfigureProperty<FrameCacheConfigColorType, EditorConfig>(nameof(FrameCacheColorType))
            .DefaultValue(FrameCacheConfigColorType.RGBA)
            .Register();
    }

    public EditorConfig()
    {
        LibraryTabDisplayModes.CollectionChanged += (_, _) => OnChanged();
    }

    public bool AutoAdjustSceneDuration
    {
        get => GetValue(AutoAdjustSceneDurationProperty);
        set => SetValue(AutoAdjustSceneDurationProperty, value);
    }

    public bool EnablePointerLockInProperty
    {
        get => GetValue(EnablePointerLockInPropertyProperty);
        set => SetValue(EnablePointerLockInPropertyProperty, value);
    }

    public bool IsAutoSaveEnabled
    {
        get => GetValue(IsAutoSaveEnabledProperty);
        set => SetValue(IsAutoSaveEnabledProperty, value);
    }

    public double FrameCacheMaxSize
    {
        get => GetValue(FrameCacheMaxSizeProperty);
        set => SetValue(FrameCacheMaxSizeProperty, value);
    }

    public FrameCacheConfigScale FrameCacheScale
    {
        get => GetValue(FrameCacheScaleProperty);
        set => SetValue(FrameCacheScaleProperty, value);
    }
    
    public FrameCacheConfigColorType FrameCacheColorType
    {
        get => GetValue(FrameCacheColorTypeProperty);
        set => SetValue(FrameCacheColorTypeProperty, value);
    }

    public CoreDictionary<string, LibraryTabDisplayMode> LibraryTabDisplayModes { get; } = new()
    {
        ["Search"] = LibraryTabDisplayMode.Show,
        ["Easings"] = LibraryTabDisplayMode.Show,
        ["Library"] = LibraryTabDisplayMode.Show,
        ["Nodes"] = LibraryTabDisplayMode.Hide,
    };

    protected override void OnPropertyChanged(PropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (args.PropertyName is not (nameof(Id) or nameof(Name)))
        {
            OnChanged();
        }
    }

    public override void Serialize(ICoreSerializationContext context)
    {
        base.Serialize(context);

        context.SetValue(nameof(LibraryTabDisplayModes), LibraryTabDisplayModes);
    }

    public override void Deserialize(ICoreSerializationContext context)
    {
        base.Deserialize(context);
        Dictionary<string, LibraryTabDisplayMode>? items
            = context.GetValue<Dictionary<string, LibraryTabDisplayMode>>(nameof(LibraryTabDisplayModes));

        if (items != null)
        {
            LibraryTabDisplayModes.Clear();
            foreach (KeyValuePair<string, LibraryTabDisplayMode> item in items)
            {
                LibraryTabDisplayModes.TryAdd(item.Key, item.Value);
            }
        }
    }

    private static ulong GetWindowsMemoryCapacity()
    {
        // wmic memorychip get capacity
        using var process = Process.Start(new ProcessStartInfo("wmic", "memorychip get capacity")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true
        });

        if (process != null && process.WaitForExit(500))
        {
            ulong value = 0;
            while (process.StandardOutput.ReadLine() is string line)
            {
                if (ulong.TryParse(line, out ulong v))
                {
                    value += v;
                }
            }

            return value;
        }

        // https://www.microsoft.com/ja-jp/windows/windows-11-specifications
        return 4L * 1024 * 1024 * 1024;
    }

    private static ulong GetLinuxMemoryCapacity()
    {
        const string FileName = "/proc/meminfo";
        // このifは多分無駄
        if (File.Exists(FileName))
        {
            foreach (string item in File.ReadLines(FileName))
            {
                if (item.StartsWith("MemTotal:"))
                {
                    string? s = item.Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
                    if (ulong.TryParse(s, out ulong v))
                    {
                        // kBで固定されている
                        // https://github.com/torvalds/linux/blob/3a5879d495b226d0404098e3564462d5f1daa33b/fs/proc/meminfo.c#L31
                        return v * 1024;
                    }
                }
            }
        }

        // https://help.ubuntu.com/community/Installation/SystemRequirements
        return 4L * 1024 * 1024 * 1024;
    }
}
