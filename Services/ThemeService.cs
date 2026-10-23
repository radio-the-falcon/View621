using Microsoft.UI.Xaml;
using System;

namespace View621.Services;

/// <summary>
/// Service for managing application theme (Light, Dark, Default) with native Windows 11 integration.
/// </summary>
public static class ThemeService
{
    private static ElementTheme _currentTheme = ElementTheme.Default;

    public static ElementTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            _currentTheme = value;
            ApplyTheme(value);
        }
    }

    public static void ApplyTheme(ElementTheme theme)
    {
        if (App.CurrentWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }
    }
}
