using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using E6Viewer.Services;

namespace E6Viewer.Views;

public sealed partial class SettingsView : Page
{
    private bool _isInitializing = true;

    public SettingsView()
    {
        InitializeComponent();
        Loaded += SettingsView_Loaded;
    }

    private void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;

        ThemeComboBox.SelectedIndex = ThemeService.CurrentTheme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0
        };

        BackdropComboBox.SelectedIndex = string.Equals(AppSettings.BackdropKind, "Acrylic", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        TranslucencySlider.Value = Math.Round(AppSettings.BackdropOpacity * 100);
        TranslucencyLabel.Text = $"{(int)TranslucencySlider.Value}%";

        PageSizeComboBox.SelectedIndex = AppSettings.PageSize switch
        {
            24 => 0,
            75 => 2,
            _ => 1
        };

        UsernameTextBox.Text = AppSettings.Username;
        ApiKeyPasswordBox.Password = AppSettings.ApiKey;

        _isInitializing = false;
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            var selectedTheme = tag switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            ThemeService.CurrentTheme = selectedTheme;
        }
    }

    private void BackdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (BackdropComboBox.SelectedItem is ComboBoxItem item && item.Tag is string kind)
        {
            AppSettings.BackdropKind = kind;
        }
    }

    private void TranslucencySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isInitializing) return;

        var val = (int)e.NewValue;
        if (TranslucencyLabel != null)
        {
            TranslucencyLabel.Text = $"{val}%";
        }
        AppSettings.BackdropOpacity = val / 100.0;
    }

    private void PageSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (PageSizeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string sizeStr && int.TryParse(sizeStr, out var size))
        {
            AppSettings.PageSize = size;
        }
    }

    private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        AppSettings.Username = UsernameTextBox.Text.Trim();
    }

    private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        AppSettings.ApiKey = ApiKeyPasswordBox.Password.Trim();
    }

    private async void OpenDataFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StorageLocations.EnsureDirectories();
            var dir = StorageLocations.DataDirectory;
            if (System.IO.Directory.Exists(dir))
            {
                var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(dir);
                await Windows.System.Launcher.LaunchFolderAsync(folder);
            }
        }
        catch
        {
            try
            {
                // Fallback to Process.Start with explorer.exe
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = StorageLocations.DataDirectory,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
