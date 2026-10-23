using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using View621.Models;
using View621.Services;

namespace View621.Views;

public sealed partial class FavouritesView : Page
{
    public ObservableCollection<ImagePost> Posts { get; } = new();
    private readonly IImageService _imageService = new E621ApiClient();
    private CancellationTokenSource? _loadCts;
    private bool _isLoading;
    private int _currentPage = 1;
    private bool _hasMore = true;

    public FavouritesView()
    {
        InitializeComponent();
        FavouritesItemsView.ItemsSource = Posts;
        Loaded += FavouritesView_Loaded;
        Unloaded += FavouritesView_Unloaded;

        AppSettings.LoginStateChanged += AppSettings_LoginStateChanged;
    }

    private void FavouritesView_Loaded(object sender, RoutedEventArgs e)
    {
        CheckLoginAndLoad();
    }

    private void FavouritesView_Unloaded(object sender, RoutedEventArgs e)
    {
        _loadCts?.Cancel();
    }

    private void AppSettings_LoginStateChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(CheckLoginAndLoad);
    }

    private void CheckLoginAndLoad()
    {
        if (!AppSettings.IsLoggedIn)
        {
            _loadCts?.Cancel();
            Posts.Clear();
            LoadingPanel.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            GridScrollViewer.Visibility = Visibility.Collapsed;
            NotLoggedInPanel.Visibility = Visibility.Visible;
            HeaderSubtitleText.Text = "Login required to access favourites";
        }
        else
        {
            NotLoggedInPanel.Visibility = Visibility.Collapsed;
            if (Posts.Count == 0 && !_isLoading)
            {
                _ = LoadFavouritesAsync(isNewQuery: true);
            }
            else
            {
                UpdateUIState();
            }
        }
    }

    private async Task LoadFavouritesAsync(bool isNewQuery)
    {
        if (!AppSettings.IsLoggedIn) return;

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        _isLoading = true;

        if (isNewQuery)
        {
            _currentPage = 1;
            _hasMore = true;
            LoadingPanel.Visibility = Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            GridScrollViewer.Visibility = Visibility.Collapsed;
            HeaderSubtitleText.Text = "Loading favourites from e621...";
        }

        try
        {
            var results = await _imageService.GetFavoritesAsync(_currentPage, AppSettings.PageSize, ct);
            if (ct.IsCancellationRequested) return;

            if (isNewQuery)
            {
                Posts.Clear();
            }

            foreach (var post in results)
            {
                post.IsFavorited = true;
                Posts.Add(post);
            }

            _hasMore = results.Count >= AppSettings.PageSize;
            LoadMoreButton.Visibility = _hasMore ? Visibility.Visible : Visibility.Collapsed;

            UpdateUIState();
            FavoritesService.Instance.SetCount(Posts.Count);
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            HeaderSubtitleText.Text = $"Failed to load favourites: {ex.Message}";
            StorageLocations.Log($"[Favourites Load Error] {ex}");
        }
        finally
        {
            _isLoading = false;
            LoadingPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateUIState()
    {
        if (!AppSettings.IsLoggedIn)
        {
            NotLoggedInPanel.Visibility = Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            GridScrollViewer.Visibility = Visibility.Collapsed;
            return;
        }

        NotLoggedInPanel.Visibility = Visibility.Collapsed;

        if (Posts.Count == 0)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            GridScrollViewer.Visibility = Visibility.Collapsed;
            HeaderSubtitleText.Text = "No favourites found";
        }
        else
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            GridScrollViewer.Visibility = Visibility.Visible;
            HeaderSubtitleText.Text = Posts.Count == 1 ? "1 favourited post" : $"{Posts.Count} favourited posts";
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppSettings.IsLoggedIn)
        {
            _ = LoadFavouritesAsync(isNewQuery: true);
        }
        else
        {
            CheckLoginAndLoad();
        }
    }

    private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (App.CurrentWindow is MainWindow mw)
        {
            mw.NavigateToSettings();
        }
    }

    private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isLoading && _hasMore)
        {
            _currentPage++;
            _ = LoadFavouritesAsync(isNewQuery: false);
        }
    }

    private async void RemoveFavourite(ImagePost post)
    {
        if (post == null || post.Id <= 0) return;

        try
        {
            var success = await _imageService.RemoveFavoriteAsync(post.Id);
            if (success)
            {
                Posts.Remove(post);
                FavoritesService.Instance.Decrement();
                UpdateUIState();
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[Remove Favourite Error] {ex.Message}");
        }
    }

    private void CardRemoveFavouriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ImagePost post })
        {
            RemoveFavourite(post);
        }
    }

    private void CardRemoveFavouriteButton_Tapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void CardRemoveFavouriteButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void CardRemoveFavourite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: ImagePost post })
        {
            RemoveFavourite(post);
        }
    }

    private void CardOpenInNewTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: ImagePost post } && App.CurrentWindow is MainWindow mw)
        {
            mw.AddPostTab(post, new PostNavigationContext
            {
                Posts = new List<ImagePost>(Posts),
                CurrentIndex = Posts.IndexOf(post),
                Mode = NavigationMode.SearchResults
            });
        }
    }

    private void CardCopyLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: ImagePost post } && !string.IsNullOrWhiteSpace(post.FullUrl))
        {
            var package = new DataPackage();
            package.SetText(post.FullUrl);
            Clipboard.SetContent(package);
        }
    }

    private void FavouritesItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is ImagePost post && App.CurrentWindow is MainWindow mw)
        {
            mw.AddPostTab(post, new PostNavigationContext
            {
                Posts = new List<ImagePost>(Posts),
                CurrentIndex = Posts.IndexOf(post),
                Mode = NavigationMode.SearchResults
            });
        }
    }

    private void Card_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Handled by FavouritesItemsView_ItemInvoked
    }

    private void Card_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Middle click opens in new tab
        var point = e.GetCurrentPoint(sender as UIElement);
        if (point.Properties.IsMiddleButtonPressed && sender is FrameworkElement { DataContext: ImagePost post } && App.CurrentWindow is MainWindow mw)
        {
            mw.AddPostTab(post, new PostNavigationContext
            {
                Posts = new List<ImagePost>(Posts),
                CurrentIndex = Posts.IndexOf(post),
                Mode = NavigationMode.SearchResults
            });
            e.Handled = true;
        }
    }

    private void ItemContainer_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Handled at Card level
    }

    private void Thumbnail_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is Image { Parent: Grid grid })
        {
            foreach (var child in grid.Children)
            {
                if (child is ProgressRing ring)
                {
                    ring.IsActive = false;
                    ring.Visibility = Visibility.Collapsed;
                    break;
                }
            }
        }
    }

    private void Thumbnail_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image { Parent: Grid grid })
        {
            foreach (var child in grid.Children)
            {
                if (child is ProgressRing ring)
                {
                    ring.IsActive = false;
                    ring.Visibility = Visibility.Collapsed;
                    break;
                }
            }
        }
    }
}
