using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace E6Viewer.Services.Navigation;

/// <summary>
/// Central registry of view modes in Umamiff.
/// </summary>
public class ViewRegistry : IViewRegistry
{
    private static IViewRegistry? _instance;
    public static IViewRegistry Instance => _instance ??= new ViewRegistry();

    public static void Initialize(IViewRegistry registry)
    {
        _instance = registry;
    }

    private readonly ConcurrentDictionary<string, ViewDescriptor> _viewsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Type, ViewDescriptor> _viewsByType = new();

    public void RegisterView(ViewDescriptor descriptor)
    {
        if (descriptor == null) return;
        _viewsById[descriptor.Id] = descriptor;
        _viewsByType[descriptor.ViewType] = descriptor;
    }

    public ViewDescriptor? GetView(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return _viewsById.TryGetValue(id, out var desc) ? desc : null;
    }

    public ViewDescriptor? GetViewForType(Type viewType)
    {
        if (viewType == null) return null;
        return _viewsByType.TryGetValue(viewType, out var desc) ? desc : null;
    }

    public IReadOnlyList<ViewDescriptor> GetAllViews()
    {
        return _viewsById.Values.ToList();
    }

    public IReadOnlyList<ViewDescriptor> GetNavPaneViews()
    {
        return _viewsById.Values.Where(v => v.IsNavPaneItem).ToList();
    }
}
