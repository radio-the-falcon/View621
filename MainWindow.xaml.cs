using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using View621.Helpers;
using View621.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace View621;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        WindowChromeHelper.InitializeTitleBar(this, AppTitleBar);
        WindowChromeHelper.SetAppIcon(AppWindow);
        #if WINDOWS
        AppWindow?.Resize(new Windows.Graphics.SizeInt32 { Width = 1100, Height = 750 });
#endif

        SyncSidebarHideSex();
        SyncSidebarRating();
        View621.Services.AppSettings.SettingsChanged += (_, _) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                SyncSidebarHideSex();
                SyncSidebarRating();
                ApplyBackdropSettings();
            });
        };

        SyncSearchHistory();
        View621.Services.AppSettings.SearchHistoryChanged += (_, _) => DispatcherQueue.TryEnqueue(SyncSearchHistory);

        SyncNavBadges();
        View621.Services.BookmarkService.Instance.BookmarksChanged += (_, _) => DispatcherQueue.TryEnqueue(SyncNavBadges);
        View621.Services.SavedSearchesService.Instance.SavedSearchesChanged += (_, _) => DispatcherQueue.TryEnqueue(SyncNavBadges);
        View621.Services.FavoritesService.Instance.FavoritesChanged += (_, _) => DispatcherQueue.TryEnqueue(SyncNavBadges);
        View621.Services.AppSettings.LoginStateChanged += (_, _) =>
        {
            View621.Services.FavoritesService.Instance.Invalidate();
            DispatcherQueue.TryEnqueue(SyncNavBadges);
        };

        NavView.PaneOpening += (s, _) => UpdateSidebarPaneDisplay(isPaneOpen: true);
        NavView.PaneClosing += (s, _) => UpdateSidebarPaneDisplay(isPaneOpen: false);
        NavView.DisplayModeChanged += (s, _) => UpdateSidebarPaneDisplay(NavView.IsPaneOpen);
        UpdateSidebarPaneDisplay(NavView.IsPaneOpen);

        // Apply backdrop & title bar colors
        ApplyBackdropSettings();
        UpdateTitleBarColors();
        if (Content is FrameworkElement fe)
        {
            fe.ActualThemeChanged += (s, _) => UpdateTitleBarColors();
        }

        // Hook up TabService singleton events
        if (View621.Services.TabService.Instance is View621.Services.TabService ts)
        {
            ts.ExploreRequested += () => { NavView.SelectedItem = NavView.MenuItems[0]; };
            ts.SearchTabRequested += (query) => AddSearchTab(query);
            ts.PostTabRequested += (post, context) => AddPostTab(post, context);
            ts.PoolTabRequested += (poolId, poolName) => AddPoolTab(poolId, poolName);
            ts.CloseCurrentTabRequested += () => CloseActiveTab();
        }

        // Add initial Explore tab
        AddSearchTab();
    }

    public void ApplyBackdropSettings()
    {
        WindowChromeHelper.ApplySystemBackdrop(
            this,
            View621.Services.AppSettings.BackdropKind,
            TintOverlayGrid,
            View621.Services.AppSettings.BackdropOpacity);
    }

    public void UpdateTitleBarColors()
    {
        var theme = (Content is FrameworkElement fe) ? fe.ActualTheme : ElementTheme.Default;
        WindowChromeHelper.UpdateTitleBarColors(AppWindow, theme);
    }

    private readonly System.Collections.ObjectModel.ObservableCollection<string> _searchHistory = new();

    private void SyncSearchHistory()
    {
        _searchHistory.Clear();
        foreach (var query in View621.Services.AppSettings.SearchHistory)
        {
            _searchHistory.Add(query);
        }

        SearchHistoryListView.ItemsSource = _searchHistory;
        NoSearchesPlaceholder.Visibility = _searchHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ClearSearchHistoryButton.IsEnabled = _searchHistory.Count > 0;
    }

    private void SyncNavBadges()
    {
        if (PinnedBadge != null)
        {
            var pinnedCount = View621.Services.BookmarkService.Instance.GetBookmarkedPostIds().Count;
            if (pinnedCount > 0)
            {
                PinnedBadge.Value = pinnedCount;
                PinnedBadge.Visibility = Visibility.Visible;
            }
            else
            {
                PinnedBadge.Visibility = Visibility.Collapsed;
            }
        }

        if (SavedBadge != null)
        {
            var savedCount = View621.Services.SavedSearchesService.Instance.GetSavedSearches().Count;
            if (savedCount > 0)
            {
                SavedBadge.Value = savedCount;
                SavedBadge.Visibility = Visibility.Visible;
            }
            else
            {
                SavedBadge.Visibility = Visibility.Collapsed;
            }
        }

        if (FavouritesBadge != null)
        {
            if (!View621.Services.AppSettings.IsLoggedIn)
            {
                FavouritesBadge.Visibility = Visibility.Collapsed;
            }
            else
            {
                var cached = View621.Services.FavoritesService.Instance.CachedCount;
                if (cached.HasValue)
                {
                    if (cached.Value > 0)
                    {
                        FavouritesBadge.Value = cached.Value;
                        FavouritesBadge.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        FavouritesBadge.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    // Asynchronously fetch count if not yet cached
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var service = new View621.Services.E621ApiClient();
                            var count = await service.GetFavoritesCountAsync();
                            if (count.HasValue)
                            {
                                DispatcherQueue.TryEnqueue(() =>
                                {
                                    View621.Services.FavoritesService.Instance.SetCount(count.Value);
                                });
                            }
                        }
                        catch
                        {
                            // Ignore network error on badge load
                        }
                    });
                }
            }
        }
    }

    private void SearchHistoryListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is string query && !string.IsNullOrWhiteSpace(query))
        {
            AddSearchTab(query);
        }
    }

    private void DeleteSearchHistoryItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string query })
        {
            View621.Services.AppSettings.RemoveSearchFromHistory(query);
        }
    }

    private void ClearSearchHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        View621.Services.AppSettings.ClearSearchHistory();
    }

    private bool _isResizingSearchHistory;
    private double _resizeStartY;
    private double _initialBorderHeight;

    private void SearchHistoryResizeHandle_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(sender as UIElement).Properties;
        if (properties.IsLeftButtonPressed && sender is UIElement element)
        {
            _isResizingSearchHistory = true;
            _resizeStartY = e.GetCurrentPoint(this.Content).Position.Y;
            _initialBorderHeight = double.IsNaN(SearchHistoryBorder.Height) ? SearchHistoryBorder.ActualHeight : SearchHistoryBorder.Height;
            element.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void SearchHistoryResizeHandle_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isResizingSearchHistory) return;

        var currentY = e.GetCurrentPoint(this.Content).Position.Y;
        var deltaY = currentY - _resizeStartY;
        // The handle is now above the previous searches container, so dragging upwards expands the container height
        var newHeight = Math.Clamp(_initialBorderHeight - deltaY, 64, 450);
        SearchHistoryBorder.Height = newHeight;
        e.Handled = true;
    }

    private void SearchHistoryResizeHandle_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isResizingSearchHistory)
        {
            _isResizingSearchHistory = false;
            (sender as UIElement)?.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    private void SearchHistoryResizeHandle_PointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isResizingSearchHistory = false;
    }

    private void UpdateSidebarPaneDisplay(bool isPaneOpen)
    {
        if (ExpandedSidebarPanel == null || CollapsedSidebarIndicators == null) return;

        if (isPaneOpen)
        {
            ExpandedSidebarPanel.Visibility = Visibility.Visible;
            CollapsedSidebarIndicators.Visibility = Visibility.Collapsed;
        }
        else
        {
            ExpandedSidebarPanel.Visibility = Visibility.Collapsed;
            CollapsedSidebarIndicators.Visibility = Visibility.Visible;
        }
    }

    private bool _isSyncingRating;

    private void SyncSidebarRating()
    {
        if (SidebarRatingComboBox == null) return;
        _isSyncingRating = true;
        SidebarRatingComboBox.SelectedIndex = View621.Services.AppSettings.RatingFilter switch
        {
            "Safe" => 0,
            "Questionable" => 1,
            _ => 2 // Explicit
        };
        _isSyncingRating = false;
    }

    private void SidebarRatingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingRating) return;

        if (SidebarRatingComboBox.SelectedItem is ComboBoxItem item && item.Tag is string rating)
        {
            View621.Services.AppSettings.RatingFilter = rating;
        }
    }

    private bool _isSyncingHideSex;

    private void SyncSidebarHideSex()
    {
        if (SidebarHideSexComboBox == null) return;
        _isSyncingHideSex = true;
        SidebarHideSexComboBox.SelectedIndex = View621.Services.AppSettings.ShowGender switch
        {
            "Male" => 1,
            "Female" => 2,
            _ => 0
        };
        _isSyncingHideSex = false;
    }

    private void SidebarHideSexComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingHideSex) return;

        if (SidebarHideSexComboBox.SelectedItem is ComboBoxItem item && item.Tag is string showGender)
        {
            View621.Services.AppSettings.ShowGender = showGender;
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    public void AddSearchTab(string? query = null, string? displayTitle = null)
    {
        var frame = new Frame();
        frame.Navigate(typeof(SearchView), query);

        string headerTitle;
        if (!string.IsNullOrWhiteSpace(displayTitle))
        {
            headerTitle = displayTitle.Trim();
        }
        else if (string.IsNullOrWhiteSpace(query))
        {
            headerTitle = "Search";
        }
        else
        {
            // Clean out order:..., rating:..., and gender filters (-female, -male) from the visual tab title
            var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cleanTokens = tokens.Where(t =>
                !t.StartsWith("order:", StringComparison.OrdinalIgnoreCase) &&
                !t.StartsWith("rating:", StringComparison.OrdinalIgnoreCase) &&
                !t.Equals("-rating:e", StringComparison.OrdinalIgnoreCase) &&
                !t.Equals("-female", StringComparison.OrdinalIgnoreCase) &&
                !t.Equals("-male", StringComparison.OrdinalIgnoreCase)
            ).ToList();

            headerTitle = cleanTokens.Count > 0 ? string.Join(" ", cleanTokens) : "Search";
        }

        if (headerTitle.Length > 20) headerTitle = headerTitle.Substring(0, 18) + "…";

        var tabItem = new TabViewItem
        {
            Header = headerTitle,
            IconSource = new FontIconSource { Glyph = "\uE721" },
            Content = frame
        };

        MainTabView.TabItems.Add(tabItem);
        MainTabView.SelectedItem = tabItem;

        // Ensure TabView is visible
        MainTabView.Visibility = Visibility.Visible;
        NavFrame.Visibility = Visibility.Collapsed;
        if (NavView.MenuItems.Count > 0 && NavView.SelectedItem != NavView.MenuItems[0])
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

    public void AddPostTab(View621.Models.ImagePost post, View621.Models.PostNavigationContext? context = null)
    {
        var frame = new Frame();
        frame.Navigate(typeof(PostView), context ?? new View621.Models.PostNavigationContext
        {
            Posts = new System.Collections.Generic.List<View621.Models.ImagePost> { post },
            CurrentIndex = 0
        });

        var tabItem = new TabViewItem
        {
            Header = $"Post #{post.Id}",
            IconSource = new FontIconSource { Glyph = "\uEB9F" },
            Content = frame
        };

        MainTabView.TabItems.Add(tabItem);
        MainTabView.SelectedItem = tabItem;

        MainTabView.Visibility = Visibility.Visible;
        NavFrame.Visibility = Visibility.Collapsed;
        if (NavView.MenuItems.Count > 0 && NavView.SelectedItem != NavView.MenuItems[0])
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

    public void AddPoolTab(long poolId, string? poolName = null)
    {
        var navContext = new View621.Models.PostNavigationContext
        {
            Mode = View621.Models.NavigationMode.Pool,
            PoolId = poolId,
            PoolName = poolName
        };

        var frame = new Frame();
        frame.Navigate(typeof(PostView), navContext);

        var tabItem = new TabViewItem
        {
            Header = string.IsNullOrWhiteSpace(poolName) ? $"Pool #{poolId}" : poolName,
            IconSource = new FontIconSource { Glyph = "\uE838" },
            Content = frame
        };

        MainTabView.TabItems.Add(tabItem);
        MainTabView.SelectedItem = tabItem;

        MainTabView.Visibility = Visibility.Visible;
        NavFrame.Visibility = Visibility.Collapsed;
        if (NavView.MenuItems.Count > 0 && NavView.SelectedItem != NavView.MenuItems[0])
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

    private void MainTabView_AddTabButtonClick(TabView sender, object args)
    {
        AddSearchTab();
    }

    private void MainTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        CloseTab(args.Tab);
    }

    public void CloseActiveTab()
    {
        if (MainTabView.SelectedItem is TabViewItem activeTab)
        {
            CloseTab(activeTab);
        }
    }

    private void CloseTab(TabViewItem tab)
    {
        if (tab.Content is Frame { Content: SearchView searchView })
        {
            searchView.Cleanup();
        }

        MainTabView.TabItems.Remove(tab);

        // If all tabs closed, automatically open a fresh search tab
        if (MainTabView.TabItems.Count == 0)
        {
            AddSearchTab();
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            MainTabView.Visibility = Visibility.Collapsed;
            NavFrame.Visibility = Visibility.Visible;
            NavFrame.Navigate(typeof(SettingsView));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "search":
                case "home":
                    NavFrame.Visibility = Visibility.Collapsed;
                    MainTabView.Visibility = Visibility.Visible;
                    break;
                case "pinned":
                case "bookmarks":
                    MainTabView.Visibility = Visibility.Collapsed;
                    NavFrame.Visibility = Visibility.Visible;
                    NavFrame.Navigate(typeof(PinView));
                    break;
                case "saved":
                case "saved_searches":
                    MainTabView.Visibility = Visibility.Collapsed;
                    NavFrame.Visibility = Visibility.Visible;
                    NavFrame.Navigate(typeof(SavedSearchesView));
                    break;
                case "favourites":
                case "favorites":
                    MainTabView.Visibility = Visibility.Collapsed;
                    NavFrame.Visibility = Visibility.Visible;
                    NavFrame.Navigate(typeof(FavouritesView));
                    break;
                case "about":
                    MainTabView.Visibility = Visibility.Collapsed;
                    NavFrame.Visibility = Visibility.Visible;
                    NavFrame.Navigate(typeof(AboutView));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown navigation item tag: {item.Tag}");
            }
        }
    }

    public void NavigateToSettings()
    {
        NavView.SelectedItem = NavView.SettingsItem;
    }
}
