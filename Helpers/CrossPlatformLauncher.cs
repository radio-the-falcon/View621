using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace E6Viewer.Helpers;

/// <summary>
/// Cross-platform file manager and URL launcher supporting Windows, macOS, and Linux.
/// </summary>
public static class CrossPlatformLauncher
{
    /// <summary>
    /// Opens the specified folder in the operating system's default file manager.
    /// </summary>
    public static async Task OpenFolderAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", $"\"{folderPath}\"");
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", $"\"{folderPath}\"");
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Fallback to Uri launch
            try
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(folderPath));
            }
            catch { }
        }
    }

    /// <summary>
    /// Opens a web URL in the system's default web browser.
    /// </summary>
    public static async Task OpenBrowserAsync(Uri uri)
    {
        if (uri == null) return;

        try
        {
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
        catch
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = uri.AbsoluteUri,
                        UseShellExecute = true
                    });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", $"\"{uri.AbsoluteUri}\"");
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", $"\"{uri.AbsoluteUri}\"");
                }
            }
            catch { }
        }
    }
}
