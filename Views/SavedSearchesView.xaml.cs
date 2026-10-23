using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using View621.Models;
using View621.ViewModels;

namespace View621.Views;

public sealed partial class SavedSearchesView : Page
{
    public SavedSearchesViewModel ViewModel { get; } = new();

    public SavedSearchesView()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Enabled;
        Loaded += SavedSearchesView_Loaded;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SavedSearchesViewModel.HasNoSavedSearches))
        {
            UpdateUIState();
        }
    }

    private void SavedSearchesView_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.ReloadRows();
        UpdateUIState();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.ReloadRows();
        UpdateUIState();
    }

    private void UpdateUIState()
    {
        if (ViewModel.HasNoSavedSearches)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            RowsScrollViewer.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            RowsScrollViewer.Visibility = Visibility.Visible;
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.RefreshAllAsync();
    }

    private void SeeMorePosts_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SavedSearchRowViewModel row })
        {
            App.CurrentWindow?.AddSearchTab(row.EffectiveQuery, row.DisplayTitle);
        }
    }

    private void DeleteSavedSearch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SavedSearchRowViewModel row })
        {
            ViewModel.DeleteSavedSearch(row);
        }
    }

    private void Thumbnail_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            var post = (fe.Tag as ImagePost) ?? (fe.DataContext as ImagePost);
            if (post != null)
            {
                App.CurrentWindow?.AddPostTab(post);
            }
        }
    }
}
