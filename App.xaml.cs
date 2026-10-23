using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace View621;

/// <summary>
/// Provides application-specific behaviour to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static MainWindow? CurrentWindow { get; private set; }

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        var msg = $"[App UnhandledException] {e.Message}\n{e.Exception}\n";
        View621.Services.StorageLocations.Log(msg);
        try { System.IO.File.AppendAllText("startup_error.log", msg); } catch { }
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        var msg = $"[CurrentDomain UnhandledException] {ex?.Message}\n{ex}\n";
        View621.Services.StorageLocations.Log(msg);
        try { System.IO.File.AppendAllText("startup_error.log", msg); } catch { }
    }

    /// <summary>
    /// Global exception handler for TaskScheduler unobserved task exceptions.
    /// </summary>
    public static void LogTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        var msg = $"[UnobservedTaskException] {e.Exception.Message}\n{e.Exception}\n";
        View621.Services.StorageLocations.Log(msg);
        try { System.IO.File.AppendAllText("startup_error.log", msg); } catch { }
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            // Ensure ~/.view621 and cache directories exist on first launch
            View621.Services.StorageLocations.EnsureDirectories();

            // Register canonical views
            View621.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new View621.Services.Navigation.ViewDescriptor("search", "Search", "\uE721", typeof(View621.Views.SearchView), IsTabCapable: true, IsNavPaneItem: true));
            View621.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new View621.Services.Navigation.ViewDescriptor("post", "Post", "\uEB9F", typeof(View621.Views.PostView), IsTabCapable: true, IsNavPaneItem: false));
            View621.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new View621.Services.Navigation.ViewDescriptor("pinned", "Pinned", "\uE718", typeof(View621.Views.PinView), IsTabCapable: false, IsNavPaneItem: true));
            View621.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new View621.Services.Navigation.ViewDescriptor("saved", "Saved Searches", "\uE74E", typeof(View621.Views.SavedSearchesView), IsTabCapable: false, IsNavPaneItem: true));
            View621.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new View621.Services.Navigation.ViewDescriptor("favourites", "Favourites", "\uE734", typeof(View621.Views.FavouritesView), IsTabCapable: false, IsNavPaneItem: true));
            View621.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new View621.Services.Navigation.ViewDescriptor("settings", "Settings", "\uE713", typeof(View621.Views.SettingsView), IsTabCapable: false, IsNavPaneItem: true));
            View621.Services.Navigation.ViewRegistry.Instance.RegisterView(
                new View621.Services.Navigation.ViewDescriptor("about", "About", "\uE946", typeof(View621.Views.AboutView), IsTabCapable: false, IsNavPaneItem: true));

            CurrentWindow = new MainWindow();
            CurrentWindow.Activate();
        }
        catch (Exception ex)
        {
            var msg = $"[OnLaunched Exception] {ex.Message}\n{ex}\n";
            View621.Services.StorageLocations.Log(msg);
            try { System.IO.File.AppendAllText("startup_error.log", msg); } catch { }
            throw;
        }
    }
}
