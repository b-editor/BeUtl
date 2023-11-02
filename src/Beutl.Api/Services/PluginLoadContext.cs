﻿using System.Reflection;
using System.Runtime.Loader;

using NuGet.Packaging;

namespace Beutl.Api.Services;

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly PluginDependencyResolver _pluginResolver;

    public PluginLoadContext(string mainDirectory, PackageFolderReader? reader = null) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(AppContext.BaseDirectory);
        _pluginResolver = new PluginDependencyResolver(mainDirectory, reader);
    }

    protected override Assembly? Load(AssemblyName name)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(name);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        if (!CoreLibraries.IncludedInRuntimeDependencies(name.Name!, name.Version))
        {
            assemblyPath = _pluginResolver.ResolveAssemblyToPath(name);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
        }

        return base.Load(name);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        libraryPath = _pluginResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
