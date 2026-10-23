using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

#if WINDOWS
using Microsoft.UI.Windowing;
#endif

namespace E6Viewer.Helpers;

/// <summary>
/// Encapsulates native window decoration, icon assignment, system backdrops, and title bar styling.
/// Works across Windows (via Windows App SDK) and non-Windows (via Uno Platform).
/// </summary>
public static class WindowChromeHelper
{
    /// <summary>
    /// Configures the window's custom title bar.
    /// </summary>
    public static void InitializeTitleBar(Window window, TitleBar titleBar)
    {
#if WINDOWS
        try
        {
            window.ExtendsContentIntoTitleBar = true;
            window.SetTitleBar(titleBar);
            if (window.AppWindow?.TitleBar != null)
            {
                window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            }
        }
        catch
        {
            // Best effort titlebar customization
        }
#endif
    }

    /// <summary>
    /// Sets the application icon using the current process executable or fallback icon file in Assets.
    /// </summary>
    public static void SetAppIcon(object? appWindow)
    {
#if WINDOWS
        if (appWindow is AppWindow win)
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    win.SetIcon(exePath);
                    return;
                }

                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                if (File.Exists(iconPath))
                {
                    win.SetIcon(iconPath);
                }
            }
            catch
            {
                // Best effort icon assignment
            }
        }
#endif
    }

    /// <summary>
    /// Applies Windows 11 Mica or Acrylic backdrop and adjusts the translucency tint overlay.
    /// </summary>
    public static void ApplySystemBackdrop(Window window, string backdropKind, FrameworkElement? tintOverlay = null, double opacity = 0.85)
    {
#if WINDOWS
        if (string.Equals(backdropKind, "Acrylic", StringComparison.OrdinalIgnoreCase))
        {
            if (window.SystemBackdrop is not DesktopAcrylicBackdrop)
            {
                window.SystemBackdrop = new DesktopAcrylicBackdrop();
            }
        }
        else
        {
            if (window.SystemBackdrop is not MicaBackdrop)
            {
                window.SystemBackdrop = new MicaBackdrop();
            }
        }
#endif

        if (tintOverlay != null)
        {
            var clamped = Math.Clamp(opacity, 0.1, 1.0);
            tintOverlay.Opacity = (1.0 - clamped) * 0.4;
        }
    }

    /// <summary>
    /// Synchronizes native title bar caption button colors based on light vs dark theme.
    /// </summary>
    public static void UpdateTitleBarColors(object? appWindow, ElementTheme actualTheme)
    {
#if WINDOWS
        if (appWindow is not AppWindow win || win.TitleBar == null) return;

        bool isLight = actualTheme == ElementTheme.Light;

        if (isLight)
        {
            // Light theme: dark caption buttons
            win.TitleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            win.TitleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            win.TitleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            win.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 100, 100, 100);
            win.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            win.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(25, 0, 0, 0);
            win.TitleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(40, 0, 0, 0);
            win.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        }
        else
        {
            // Dark theme: light caption buttons
            win.TitleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            win.TitleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            win.TitleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            win.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 160, 160, 160);
            win.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            win.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(25, 255, 255, 255);
            win.TitleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(40, 255, 255, 255);
            win.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        }
#endif
    }
}
