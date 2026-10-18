using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using E6Viewer.Models;
using E6Viewer.Services;
using E6Viewer.ViewModels;

namespace E6Viewer.Views;

public sealed partial class SearchView : Page
{
    public SearchViewModel ViewModel { get; } = new();

    public SearchView()
    {
        InitializeComponent();
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        Loaded += SearchView_Loaded;
        KeyDown += SearchView_KeyDown;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        SavedSearchesService.Instance.SavedSearchesChanged += SavedSearchesService_SavedSearchesChanged;
        SyncSortMenu();
        SyncSaveButtonState();
    }

    private void SavedSearchesService_SavedSearchesChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(SyncSaveButtonState);
    }

    private void SearchView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.F5)
        {
            e.Handled = true;
            _ = ViewModel.RefreshAsync();
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.RefreshAsync();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            // Restoring previous state when user clicks Back from PostView
            if (SearchBox != null && !string.Equals(SearchBox.Text, ViewModel.SearchQuery, StringComparison.Ordinal))
            {
                SearchBox.Text = ViewModel.SearchQuery ?? string.Empty;
            }
            return;
        }

        if (e.Parameter is string query && !string.IsNullOrWhiteSpace(query))
        {
            ViewModel.SearchQuery = query;
            if (SearchBox != null)
            {
                SearchBox.Text = query;
            }
            _ = ViewModel.SearchAsync(resetPage: true).ContinueWith(_ =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (SearchBox != null && SearchBox.Text != ViewModel.SearchQuery)
                    {
                        SearchBox.Text = ViewModel.SearchQuery ?? string.Empty;
                    }
                });
            });
        }

        SyncSaveButtonState();
    }

    private void SearchView_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Posts.Count == 0 && !ViewModel.IsLoading)
        {
            _ = ViewModel.SearchAsync(resetPage: true);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SearchViewModel.SortKey) or nameof(SearchViewModel.IsSortReversed))
        {
            SyncSortMenu();
            SyncSaveButtonState();
        }
        else if (e.PropertyName is nameof(SearchViewModel.SearchQuery) or nameof(SearchViewModel.Posts))
        {
            SyncSaveButtonState();
        }
    }

    private void SyncSaveButtonState()
    {
        if (SaveSearchButton == null || SaveSearchIcon == null) return;

        var effectiveQuery = ViewModel.BuildEffectiveQuery();
        var isSaved = !string.IsNullOrWhiteSpace(effectiveQuery) &&
                      SavedSearchesService.Instance.IsSaved(effectiveQuery);

        if (isSaved)
        {
            SaveSearchIcon.Glyph = "\uE735"; // Star / filled bookmark / saved
            ToolTipService.SetToolTip(SaveSearchButton, "Remove from saved searches");
        }
        else
        {
            SaveSearchIcon.Glyph = "\uE734"; // Outline star / bookmark
            ToolTipService.SetToolTip(SaveSearchButton, "Save this search");
        }
    }

    private void SaveSearchButton_Click(object sender, RoutedEventArgs e)
    {
        var effectiveQuery = ViewModel.BuildEffectiveQuery();
        if (string.IsNullOrWhiteSpace(effectiveQuery)) return;

        var isSaved = SavedSearchesService.Instance.IsSaved(effectiveQuery);
        if (isSaved)
        {
            SavedSearchesService.Instance.RemoveSavedSearch(effectiveQuery);
        }
        else
        {
            // Clean title: what was typed into the search box, without background order or hidden filters
            var displayTitle = SearchBox?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(displayTitle))
            {
                displayTitle = ViewModel.SearchQuery?.Trim();
            }
            if (string.IsNullOrWhiteSpace(displayTitle))
            {
                displayTitle = "Everything";
            }

            // Initial save with current post count or null
            SavedSearchesService.Instance.SaveSearch(
                displayTitle: displayTitle,
                effectiveQuery: effectiveQuery,
                sortKey: ViewModel.SortKey,
                isSortReversed: ViewModel.IsSortReversed,
                cachedCount: null);

            // Fetch total count in background and update cached count
            _ = Task.Run(async () =>
            {
                var totalCount = await new E621ApiClient().GetPostCountAsync(effectiveQuery);
                if (totalCount.HasValue)
                {
                    SavedSearchesService.Instance.SaveSearch(
                        displayTitle: displayTitle,
                        effectiveQuery: effectiveQuery,
                        sortKey: ViewModel.SortKey,
                        isSortReversed: ViewModel.IsSortReversed,
                        cachedCount: totalCount.Value);
                }
            });
        }

        SyncSaveButtonState();
    }

    // Keeps the checked state of the sort menu in step with the view model (e.g. when an order: term is typed)
    private void SyncSortMenu()
    {
        SortDefaultItem.IsChecked = string.IsNullOrEmpty(ViewModel.SortKey) || ViewModel.SortKey == "id";
        SortCreatedItem.IsChecked = ViewModel.SortKey == "created";
        SortScoreItem.IsChecked = ViewModel.SortKey == "score";
        SortFavCountItem.IsChecked = ViewModel.SortKey == "favcount";
        SortReverseItem.IsChecked = ViewModel.IsSortReversed;
    }

    private void SortItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem { Tag: string key })
        {
            ViewModel.SetSort(key);
        }
    }

    private void SortReverse_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetSortReversed(SortReverseItem.IsChecked);
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            _ = ViewModel.UpdateSuggestionsAsync(sender.Text);
        }
    }

    private string? _selectedSuggestion;

    private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        // When navigating up/down in the dropdown, record the currently highlighted item without modifying the textbox text
        if (args.SelectedItem is string selectedTag)
        {
            _selectedSuggestion = selectedTag;
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // If a suggestion from the dropdown was chosen / selected via Enter
        if (args.ChosenSuggestion is string chosenTag)
        {
            var updatedText = ViewModel.CompleteQueryWithTag(chosenTag);
            sender.Text = updatedText;
            ViewModel.SearchQuery = updatedText;
            _selectedSuggestion = null;
            ViewModel.TagSuggestions.Clear();
            return;
        }
        else if (!string.IsNullOrWhiteSpace(_selectedSuggestion))
        {
            var updatedText = ViewModel.CompleteQueryWithTag(_selectedSuggestion);
            sender.Text = updatedText;
            ViewModel.SearchQuery = updatedText;
            _selectedSuggestion = null;
            ViewModel.TagSuggestions.Clear();
            return;
        }

        ViewModel.SearchQuery = sender.Text;
        _selectedSuggestion = null;
        ViewModel.TagSuggestions.Clear();
        _ = ViewModel.SearchAsync(resetPage: true);
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (SearchBox != null)
        {
            ViewModel.SearchQuery = SearchBox.Text;
        }
        _selectedSuggestion = null;
        ViewModel.TagSuggestions.Clear();
        _ = ViewModel.SearchAsync(resetPage: true);
    }

    private void SearchBox_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            if (ViewModel.TagSuggestions.Count > 0)
            {
                ViewModel.TagSuggestions.Clear();
                _selectedSuggestion = null;
                e.Handled = true;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (!string.IsNullOrWhiteSpace(_selectedSuggestion))
            {
                var updatedText = ViewModel.CompleteQueryWithTag(_selectedSuggestion);
                SearchBox.Text = updatedText;
                ViewModel.SearchQuery = updatedText;
                _selectedSuggestion = null;
                ViewModel.TagSuggestions.Clear();
                e.Handled = true;
            }
        }
    }

    private void TagChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            ViewModel.SelectTag(tag);
        }
    }

    private void ImagesItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is ImagePost post)
        {
            NavigateToDetail(post);
        }
    }

    private void ItemContainer_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ImagePost post)
        {
            NavigateToDetail(post);
        }
    }

    private void Card_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ImagePost post)
        {
            NavigateToDetail(post);
        }
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
        if (post != null)
        {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(post.FullUrl);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        }
    }

    private void CardBookmarkButton_Click(object sender, RoutedEventArgs e)
    {
        var post = (sender as FrameworkElement)?.Tag as ImagePost ?? (sender as FrameworkElement)?.DataContext as ImagePost;
        if (post != null)
        {
            BookmarkService.Instance.ToggleBookmark(post.Id, post);
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
            BookmarkService.Instance.ToggleBookmark(post.Id, post);
        }
    }

    private PostNavigationContext CreateNavigationContext(ImagePost post)
    {
        var postsSnapshot = new System.Collections.Generic.List<ImagePost>(ViewModel.Posts);
        var index = postsSnapshot.FindIndex(p => p.Id == post.Id);
        if (index < 0) index = 0;

        return new PostNavigationContext
        {
            Mode = NavigationMode.SearchResults,
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
            System.IO.File.AppendAllText("startup_error.log", $"[Navigation Error] {ex}\n");
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
