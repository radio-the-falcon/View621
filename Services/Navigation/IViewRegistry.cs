using System;
using System.Collections.Generic;

namespace E6Viewer.Services.Navigation;

/// <summary>
/// Descriptor representing a top-level or tab-capable view mode.
/// </summary>
public record ViewDescriptor(
    string Id,
    string DisplayName,
    string Glyph,
    Type ViewType,
    bool IsTabCapable = true,
    bool IsNavPaneItem = false
);

/// <summary>
/// Registry of view modes in the application, enabling easy registration of new views (e.g. SavedSearchesView).
/// </summary>
public interface IViewRegistry
{
    void RegisterView(ViewDescriptor descriptor);
    ViewDescriptor? GetView(string id);
    ViewDescriptor? GetViewForType(Type viewType);
    IReadOnlyList<ViewDescriptor> GetAllViews();
    IReadOnlyList<ViewDescriptor> GetNavPaneViews();
}
