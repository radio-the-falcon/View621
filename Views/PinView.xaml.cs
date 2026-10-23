using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using View621.Models;
using View621.Services;

namespace View621.Views;

public sealed partial class PinView : Page
{
    public ObservableCollection<ImagePost> Posts { get; } = new();
    private CancellationTokenSource? _loadCts;
    private bool _isLoading;

    public PinView()
    {
        InitializeComponent();
        BookmarksItemsView.ItemsSource = Posts;
        Loaded += PinView_Loaded;
        Unloaded += PinView_Unloaded;

        BookmarkService.Instance.BookmarksChanged += BookmarkService_BookmarksChanged;
    }

    private void PinView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Posts.Count == 0 && !_isLoading)
        {
            _ = LoadBookmarksAsync();
        }
    }

    private void PinView_Unloaded(object sender, RoutedEventArgs e)
    {
        _loadCts?.Cancel();
    }

    private void BookmarkService_BookmarksChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var bookmarkedIdSet = new HashSet<long>(BookmarkService.Instance.GetBookmarkedPostIds());

            // Remove unbookmarked posts
            for (int i = Posts.Count - 1; i >= 0; i--)
            {
                if (!bookmarkedIdSet.Contains(Posts[i].Id))
                {
                    Posts.RemoveAt(i);
                }
            }

            UpdateUIState();
        });
    }

    private async Task LoadBookmarksAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        _isLoading = true;
        LoadingPanel.Visibility = Visibility.Visible;
        EmptyStatePanel.Visibility = Visibility.Collapsed;
        GridScrollViewer.Visibility = Visibility.Collapsed;
        HeaderSubtitleText.Text = "Loading pinned posts...";

        try
        {
            var results = await BookmarkService.Instance.GetBookmarkedPostsAsync(ct);
            if (ct.IsCancellationRequested) return;

            Posts.Clear();
            foreach (var post in results)
            {
                Posts.Add(post);
            }

            UpdateUIState();
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            HeaderSubtitleText.Text = $"Failed to load pinned posts: {ex.Message}";
            StorageLocations.Log($"[Bookmarks Load Error] {ex}");
        }
        finally
        {
            _isLoading = false;
            LoadingPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateUIState()
    {
        if (Posts.Count == 0)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            GridScrollViewer.Visibility = Visibility.Collapsed;
            HeaderSubtitleText.Text = "No pinned posts";
            ClearAllButton.IsEnabled = false;
        }
        else
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            GridScrollViewer.Visibility = Visibility.Visible;
            HeaderSubtitleText.Text = Posts.Count == 1 ? "1 pinned post" : $"{Posts.Count} pinned posts";
            ClearAllButton.IsEnabled = true;
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadBookmarksAsync();
    }

    private void ConfirmClearAll_Click(object sender, RoutedEventArgs e)
    {
        BookmarkService.Instance.ClearAllBookmarks();
        Posts.Clear();
        UpdateUIState();
    }

    private void ExploreButton_Click(object sender, RoutedEventArgs e)
    {
        TabService.Instance.NavigateToExplore();
    }

    private void BookmarksItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is ImagePost post)
        {
            NavigateToDetail(post);
        }
    }

    private void ItemContainer_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        // Handled by BookmarksItemsView_ItemInvoked
    }

    private void Card_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        // Handled by BookmarksItemsView_ItemInvoked
    }

    private void Card_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(sender as UIElement).Properties;
        if (properties.IsMiddleButtonPressed && sender is FrameworkElement fe && fe.DataContext is ImagePost post)
        {
            e.Handled = true;
            OpenInNewTab(post);
        }
    }

    private void CardOpenInNewTab_Click(object sender, RoutedEventArgs e)
    {
        var post = (sender as FrameworkElement)?.Tag as ImagePost ?? (sender as FrameworkElement)?.DataContext as ImagePost;
        if (post != null)
        {
            OpenInNewTab(post);
        }
    }

    private void CardCopyLink_Click(object sender, RoutedEventArgs e)
    {
        var post = (sender as FrameworkElement)?.Tag as ImagePost ?? (sender as FrameworkElement)?.DataContext as ImagePost;
        if (post != null && !string.IsNullOrWhiteSpace(post.FullUrl))
        {
            var package = new DataPackage();
            package.SetText(post.FullUrl);
            Clipboard.SetContent(package);
        }
    }

    private void CardBookmarkButton_Click(object sender, RoutedEventArgs e)
    {
        var post = (sender as FrameworkElement)?.Tag as ImagePost ?? (sender as FrameworkElement)?.DataContext as ImagePost;
        if (post != null)
        {
            BookmarkService.Instance.RemoveBookmark(post.Id);
            Posts.Remove(post);
            UpdateUIState();
        }
    }

    private void CardBookmarkButton_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void CardBookmarkButton_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void CardBookmark_Click(object sender, RoutedEventArgs e)
    {
        var post = (sender as FrameworkElement)?.Tag as ImagePost ?? (sender as FrameworkElement)?.DataContext as ImagePost;
        if (post != null)
        {
            BookmarkService.Instance.RemoveBookmark(post.Id);
            Posts.Remove(post);
            UpdateUIState();
        }
    }

    private PostNavigationContext CreateNavigationContext(ImagePost post)
    {
        var postsSnapshot = new List<ImagePost>(Posts);
        var index = postsSnapshot.FindIndex(p => p.Id == post.Id);
        if (index < 0) index = 0;

        return new PostNavigationContext
        {
            Mode = View621.Models.NavigationMode.SearchResults,
            Posts = postsSnapshot,
            CurrentIndex = index
        };
    }

    private void NavigateToDetail(ImagePost post)
    {
        try
        {
            var context = CreateNavigationContext(post);
            if (Frame != null)
            {
                Frame.Navigate(typeof(PostView), context);
            }
            else
            {
                TabService.Instance.OpenPostInNewTab(post, context);
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText("startup_error.log", $"[Navigation Error] {ex}\n");
        }
    }

    private void OpenInNewTab(ImagePost post)
    {
        var context = CreateNavigationContext(post);
        TabService.Instance.OpenPostInNewTab(post, context);
    }

    private void Thumbnail_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is Image img && img.Parent is Grid grid)
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
        if (sender is Image img)
        {
            img.Visibility = Visibility.Collapsed;
            if (img.Parent is Grid grid)
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
}
