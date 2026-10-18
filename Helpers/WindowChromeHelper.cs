using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace E6Viewer.Helpers;

/// <summary>
/// Encapsulates native Win32 and WinUI window decoration, icon assignment, system backdrops, and title bar styling.
/// </summary>
public static class WindowChromeHelper
{
    /// <summary>
    /// Configures the window's custom title bar with the preferred height.
    /// </summary>
    public static void InitializeTitleBar(Window window, TitleBar titleBar, TitleBarHeightOption heightOption = TitleBarHeightOption.Tall)
    {
        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(titleBar);
        window.AppWindow.TitleBar.PreferredHeightOption = heightOption;
    }

    /// <summary>
    /// Sets the application icon using the current process executable or fallback icon file in Assets.
    /// </summary>
    public static void SetAppIcon(AppWindow appWindow)
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                appWindow.SetIcon(exePath);
                return;
            }

            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }
        }
        catch
        {
            // Best effort icon assignment
        }
    }

    /// <summary>
    /// Applies Windows 11 Mica or Acrylic backdrop and adjusts the translucency tint overlay.
    /// </summary>
    public static void ApplySystemBackdrop(Window window, string backdropKind, FrameworkElement? tintOverlay = null, double opacity = 0.85)
    {
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

        if (tintOverlay != null)
        {
            var clamped = Math.Clamp(opacity, 0.1, 1.0);
            tintOverlay.Opacity = (1.0 - clamped) * 0.4;
        }
    }

    /// <summary>
    /// Synchronizes native title bar caption button colors based on light vs dark theme.
    /// </summary>
    public static void UpdateTitleBarColors(AppWindow appWindow, ElementTheme actualTheme)
    {
        if (appWindow?.TitleBar == null) return;

        bool isLight = actualTheme == ElementTheme.Light;

        if (isLight)
        {
            // Light theme: dark caption buttons
            appWindow.TitleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            appWindow.TitleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            appWindow.TitleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            appWindow.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 100, 100, 100);
            appWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            appWindow.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(25, 0, 0, 0);
            appWindow.TitleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(40, 0, 0, 0);
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        }
        else
        {
            // Dark theme: light caption buttons
            appWindow.TitleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            appWindow.TitleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            appWindow.TitleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            appWindow.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 160, 160, 160);
            appWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            appWindow.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(25, 255, 255, 255);
            appWindow.TitleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(40, 255, 255, 255);
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        }
    }
}
