using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace E6Viewer;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static MainWindow? CurrentWindow { get; private set; }
    
    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();

        UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var msg = $"[WinUI UnhandledException] {e.Message}\n{e.Exception}\n";
        E6Viewer.Services.StorageLocations.Log(msg);
        try { System.IO.File.AppendAllText("startup_error.log", msg); } catch { }
        Debug.WriteLine($"[WinUI UnhandledException] {e.Message} - {e.Exception}");
        // Mark as handled to prevent abrupt application crash on image decode or rendering errors
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        var msg = $"[AppDomain UnhandledException] {e.ExceptionObject}\n";
        E6Viewer.Services.StorageLocations.Log(msg);
        try { System.IO.File.AppendAllText("startup_error.log", msg); } catch { }
        Debug.WriteLine($"[AppDomain UnhandledException] {e.ExceptionObject}");
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var msg = $"[TaskScheduler UnobservedTaskException] {e.Exception}\n";
        E6Viewer.Services.StorageLocations.Log(msg);
        try { System.IO.File.AppendAllText("startup_error.log", msg); } catch { }
        Debug.WriteLine($"[TaskScheduler UnobservedTaskException] {e.Exception}");
        e.SetObserved();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            // Ensure ~/.view621 and cache directories exist on first launch
            E6Viewer.Services.StorageLocations.EnsureDirectories();

            // Register canonical views
            E6Viewer.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new E6Viewer.Services.Navigation.ViewDescriptor("search", "Search", "\uE721", typeof(E6Viewer.Views.SearchView), IsTabCapable: true, IsNavPaneItem: true));
            E6Viewer.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new E6Viewer.Services.Navigation.ViewDescriptor("post", "Post", "\uEB9F", typeof(E6Viewer.Views.PostView), IsTabCapable: true, IsNavPaneItem: false));
            E6Viewer.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new E6Viewer.Services.Navigation.ViewDescriptor("pinned", "Pinned", "\uE718", typeof(E6Viewer.Views.PinView), IsTabCapable: false, IsNavPaneItem: true));
            E6Viewer.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new E6Viewer.Services.Navigation.ViewDescriptor("saved", "Saved Searches", "\uE74E", typeof(E6Viewer.Views.SavedSearchesView), IsTabCapable: false, IsNavPaneItem: true));
            E6Viewer.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new E6Viewer.Services.Navigation.ViewDescriptor("settings", "Settings", "\uE713", typeof(E6Viewer.Views.SettingsView), IsTabCapable: false, IsNavPaneItem: true));
            E6Viewer.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new E6Viewer.Services.Navigation.ViewDescriptor("about", "About", "\uE946", typeof(E6Viewer.Views.AboutView), IsTabCapable: false, IsNavPaneItem: true));

            CurrentWindow = new MainWindow();
            CurrentWindow.Activate();
        }
        catch (Exception ex)
        {
            var msg = $"[OnLaunched Exception] {ex.Message}\n{ex}\n";
            E6Viewer.Services.StorageLocations.Log(msg);
            try { System.IO.File.AppendAllText("startup_error.log", msg); } catch { }
            throw;
        }
    }
}
