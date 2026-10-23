using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using View621.Models;
using View621.Services;

namespace View621.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly IImageService _imageService;
    private CancellationTokenSource? _suggestCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearchQuery))]
    public partial string SearchQuery { get; set; } = string.Empty;

    public bool HasSearchQuery => !string.IsNullOrEmpty(SearchQuery);

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ImagePost? SelectedPost { get; set; }

    [ObservableProperty]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    public partial bool HasPreviousPage { get; set; }

    [ObservableProperty]
    public partial bool HasNextPage { get; set; }

    /// <summary>
    /// Sort field key ("id", "created", "score", "favcount") or empty for the site default.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SortLabel))]
    public partial string SortKey { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SortLabel))]
    public partial bool IsSortReversed { get; set; }

    public ObservableCollection<ImagePost> Posts { get; } = new();
    public ObservableCollection<string> TagSuggestions { get; } = new();

    // Key -> (label, reversedLabel, normal term, reversed term).
    private static readonly (string Key, string NormalLabel, string ReversedLabel, string Normal, string Reversed)[] SortOptions =
    {
        ("id", "Default (Newest ID first)", "Oldest ID first", "order:id_desc", "order:id_asc"),
        ("created", "Date created (Newest first)", "Date created (Oldest first)", "order:created", "order:created_asc"),
        ("score", "Score (Highest first)", "Score (Lowest first)", "order:score", "order:score_asc"),
        ("favcount", "Favourites (Most first)", "Favourites (Fewest first)", "order:favcount", "order:favcount_asc"),
    };

    public string SortLabel
    {
        get
        {
            var key = string.IsNullOrEmpty(SortKey) ? "id" : SortKey;
            var option = SortOptions.FirstOrDefault(o => o.Key == key);
            if (option.Key is null) return "Default (Newest ID first)";
            return IsSortReversed ? option.ReversedLabel : option.NormalLabel;
        }
    }

    /// <summary>
    /// Returns true if the token is one of the managed order terms and updates the sort state accordingly.
    /// </summary>
    private bool TryApplySortToken(string token)
    {
        foreach (var option in SortOptions)
        {
            if (token.Equals(option.Normal, StringComparison.OrdinalIgnoreCase))
            {
                SortKey = option.Key;
                IsSortReversed = false;
                return true;
            }

            if (token.Equals(option.Reversed, StringComparison.OrdinalIgnoreCase))
            {
                SortKey = option.Key;
                IsSortReversed = true;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes managed order terms from the visible search text, moving them into the sort state.
    /// </summary>
    private void ExtractSortTokens()
    {
        var kept = new System.Collections.Generic.List<string>();
        foreach (var token in SearchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TryApplySortToken(token))
            {
                kept.Add(token);
            }
        }

        var cleaned = string.Join(" ", kept);
        // Preserve a trailing space while the user is typing; otherwise only normalise when tokens were removed.
        if (!string.Equals(cleaned, SearchQuery.Trim(), StringComparison.Ordinal))
        {
            SearchQuery = cleaned;
        }
    }

    public string BuildEffectiveQuery()
    {
        var terms = new System.Collections.Generic.List<string>();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            terms.Add(SearchQuery.Trim());
        }

        var option = SortOptions.FirstOrDefault(o => o.Key == SortKey);
        if (option.Key is not null)
        {
            var sortTerm = IsSortReversed ? option.Reversed : option.Normal;
            terms.Add(sortTerm);
        }

        if (string.Equals(AppSettings.ShowGender, "Male", StringComparison.OrdinalIgnoreCase))
        {
            // Male (-female)
            terms.Add("-female");
        }
        else if (string.Equals(AppSettings.ShowGender, "Female", StringComparison.OrdinalIgnoreCase))
        {
            // Female (-male)
            terms.Add("-male");
        }

        // Rating filter: Safe = rating:s; Questionable = -rating:e (shows safe & questionable); Explicit = all
        if (string.Equals(AppSettings.RatingFilter, "Safe", StringComparison.OrdinalIgnoreCase))
        {
            terms.Add("rating:s");
        }
        else if (string.Equals(AppSettings.RatingFilter, "Questionable", StringComparison.OrdinalIgnoreCase))
        {
            terms.Add("-rating:e");
        }

        return string.Join(" ", terms);
    }

    public void SetSort(string key)
    {
        // When Default (ID) is selected, default to descending order (order:id_desc)
        if (string.Equals(key, "id", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(key))
        {
            SortKey = "id";
            IsSortReversed = false;
            AppSettings.IsSortReversed = false;
        }
        else
        {
            SortKey = key;
        }

        AppSettings.LastSortKey = SortKey;
        _ = SearchAsync(resetPage: true);
    }

    public void SetSortReversed(bool reversed)
    {
        IsSortReversed = reversed;
        AppSettings.IsSortReversed = reversed;
        _ = SearchAsync(resetPage: true);
    }

    public SearchViewModel(IImageService imageService)
    {
        _imageService = imageService;

        // Sorting is persisted globally, but SearchQuery starts empty for each new tab
        SortKey = AppSettings.LastSortKey;
        IsSortReversed = AppSettings.IsSortReversed;

        AppSettings.FilterSettingsChanged += OnFilterSettingsChanged;
        BookmarkService.Instance.BookmarksChanged += OnBookmarksChanged;
    }

    private void OnFilterSettingsChanged(object? sender, EventArgs e)
    {
        _ = SearchAsync(resetPage: true, forceRefresh: true);
    }

    private void OnBookmarksChanged(object? sender, EventArgs e)
    {
        SyncBookmarkStates();
    }

    public void Cleanup()
    {
        AppSettings.FilterSettingsChanged -= OnFilterSettingsChanged;
        BookmarkService.Instance.BookmarksChanged -= OnBookmarksChanged;
    }

    public void SyncBookmarkStates()
    {
        foreach (var post in Posts)
        {
            post.IsBookmarked = BookmarkService.Instance.IsBookmarked(post.Id);
        }
    }

    public SearchViewModel() : this(new E621ApiClient())
    {
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await SearchAsync(resetPage: false, forceRefresh: true);
    }

    [RelayCommand]
    public async Task SearchAsync(bool resetPage = true)
    {
        await SearchAsync(resetPage, forceRefresh: false);
    }

    public async Task SearchAsync(bool resetPage, bool forceRefresh)
    {
        // Move any typed order: terms out of the visible text and into the sort state
        ExtractSortTokens();
        AppSettings.LastSearchQuery = SearchQuery;
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            AppSettings.AddSearchToHistory(SearchQuery);
        }

        if (IsLoading) return;

        if (resetPage)
        {
            CurrentPage = 1;
        }

        var effectiveQuery = BuildEffectiveQuery();
        var pageSize = AppSettings.PageSize;

        // Check search results cache if not forcing a refresh
        if (!forceRefresh && SearchCacheService.Instance.TryGetSearchResults(effectiveQuery, CurrentPage, pageSize, out var cachedResults) && cachedResults != null)
        {
            Posts.Clear();
            foreach (var post in cachedResults)
            {
                post.IsBookmarked = BookmarkService.Instance.IsBookmarked(post.Id);
                Posts.Add(post);
            }

            HasPreviousPage = CurrentPage > 1;
            HasNextPage = cachedResults.Count >= pageSize;

            StatusMessage = Posts.Count == 0
                ? "No images found for this query."
                : $"Page {CurrentPage} • {Posts.Count} images loaded (cached)";
            return;
        }

        if (resetPage)
        {
            Posts.Clear();
        }

        IsLoading = true;
        StatusMessage = "Fetching images from provider...";

        try
        {
            var results = await _imageService.SearchPostsAsync(effectiveQuery, CurrentPage, pageSize);
            Posts.Clear();
            foreach (var post in results)
            {
                post.IsBookmarked = BookmarkService.Instance.IsBookmarked(post.Id);
                PostCacheService.Instance.CachePost(post);
                Posts.Add(post);
            }

            // Cache the retrieved search results for subsequent viewings
            SearchCacheService.Instance.SetSearchResults(effectiveQuery, CurrentPage, pageSize, results);

            HasPreviousPage = CurrentPage > 1;
            HasNextPage = results.Count >= pageSize;

            StatusMessage = Posts.Count == 0
                ? "No images found for this query."
                : $"Page {CurrentPage} • {Posts.Count} images loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load images: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        CurrentPage++;
        await SearchAsync(resetPage: false);
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await SearchAsync(resetPage: false);
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = string.Empty;
        _ = SearchAsync(resetPage: true);
    }

    [RelayCommand]
    public void SelectTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchQuery = tag;
        }
        else if (!SearchQuery.Split(' ').Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            SearchQuery = $"{SearchQuery.Trim()} {tag}";
        }

        _ = SearchAsync(resetPage: true);
    }

    public async Task UpdateSuggestionsAsync(string currentText)
    {
        _suggestCts?.Cancel();
        _suggestCts = new CancellationTokenSource();
        var ct = _suggestCts.Token;

        try
        {
            if (string.IsNullOrWhiteSpace(currentText))
            {
                TagSuggestions.Clear();
                return;
            }

            // If the query ends with whitespace, the user has completed a tag and started a new one
            if (currentText.EndsWith(' '))
            {
                TagSuggestions.Clear();
                return;
            }

            // Extract the last active tag being typed (after the last space)
            var lastWord = currentText.Split(' ').LastOrDefault();
            if (string.IsNullOrWhiteSpace(lastWord) || lastWord.Length < 2)
            {
                TagSuggestions.Clear();
                return;
            }

            // Quick debounce
            await Task.Delay(150, ct);

            var matches = await _imageService.AutocompleteTagsAsync(lastWord, ct);
            if (ct.IsCancellationRequested) return;

            TagSuggestions.Clear();
            foreach (var match in matches)
            {
                TagSuggestions.Add(match);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on fast typing
        }
        catch
        {
            TagSuggestions.Clear();
        }
    }

    public string CompleteQueryWithTag(string selectedTag)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return selectedTag + " ";
        }

        // If user already typed a trailing space, append the new tag
        if (SearchQuery.EndsWith(' '))
        {
            return SearchQuery + selectedTag + " ";
        }

        // Otherwise replace the last active tag token being completed
        var lastSpaceIdx = SearchQuery.LastIndexOf(' ');
        if (lastSpaceIdx >= 0)
        {
            var prefix = SearchQuery.Substring(0, lastSpaceIdx + 1);
            return prefix + selectedTag + " ";
        }

        return selectedTag + " ";
    }
}
