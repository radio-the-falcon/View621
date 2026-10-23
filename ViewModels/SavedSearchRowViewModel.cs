using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using View621.Models;
using View621.Services;

namespace View621.ViewModels;

/// <summary>
/// View model representing an individual saved search row within SavedSearchesView.
/// </summary>
public class SavedSearchRowViewModel : INotifyPropertyChanged
{
    private readonly ISavedSearchesService _savedSearchesService;
    private readonly ISearchCacheService _searchCacheService;
    private readonly IImageService _apiClient;

    public SavedSearchEntry Entry { get; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(Entry.DisplayTitle) ? Entry.EffectiveQuery : Entry.DisplayTitle;

    public string EffectiveQuery => Entry.EffectiveQuery;

    private int? _cachedCount;
    public int? CachedCount
    {
        get => _cachedCount;
        set
        {
            if (_cachedCount != value)
            {
                _cachedCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedCount));
                OnPropertyChanged(nameof(HasCount));
            }
        }
    }

    public bool HasCount => CachedCount.HasValue;

    public string FormattedCount => CachedCount.HasValue ? $"{CachedCount.Value:N0} results" : string.Empty;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ImagePost> PreviewPosts { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public SavedSearchRowViewModel(
        SavedSearchEntry entry,
        ISavedSearchesService? savedSearchesService = null,
        ISearchCacheService? searchCacheService = null,
        IImageService? apiClient = null)
    {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        _cachedCount = entry.CachedCount;
        _savedSearchesService = savedSearchesService ?? SavedSearchesService.Instance;
        _searchCacheService = searchCacheService ?? SearchCacheService.Instance;
        _apiClient = apiClient ?? new E621ApiClient();
    }

    /// <summary>
    /// Loads the preview posts for this row, checking cache first unless forceRefresh is true.
    /// </summary>
    public async Task LoadPreviewAsync(bool forceRefresh = false)
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            const int previewLimit = 8;
            const int page = 1;

            if (!forceRefresh && _searchCacheService.TryGetSearchResults(EffectiveQuery, page, previewLimit, out var cachedResults) && cachedResults != null)
            {
                PreviewPosts.Clear();
                foreach (var post in cachedResults)
                {
                    PreviewPosts.Add(post);
                }

                // If count was previously just the preview batch size (e.g. <= 8), fetch actual total count
                if (!CachedCount.HasValue || CachedCount.Value <= previewLimit)
                {
                    var totalCount = await _apiClient.GetPostCountAsync(EffectiveQuery);
                    if (totalCount.HasValue)
                    {
                        CachedCount = totalCount.Value;
                        _savedSearchesService.UpdateCachedCount(Entry.Id, totalCount.Value);
                    }
                }

                return;
            }

            // Fresh network call for preview posts
            var posts = await _apiClient.SearchPostsAsync(EffectiveQuery, page, previewLimit);
            _searchCacheService.SetSearchResults(EffectiveQuery, page, previewLimit, posts);

            PreviewPosts.Clear();
            foreach (var post in posts)
            {
                PreviewPosts.Add(post);
            }

            // Fetch total result count for this query
            var queryTotalCount = await _apiClient.GetPostCountAsync(EffectiveQuery);
            if (queryTotalCount.HasValue)
            {
                CachedCount = queryTotalCount.Value;
                _savedSearchesService.UpdateCachedCount(Entry.Id, queryTotalCount.Value);
            }
            else if (!CachedCount.HasValue && posts.Count < previewLimit)
            {
                // Only if total results returned is less than preview limit do we know the exact total
                CachedCount = posts.Count;
                _savedSearchesService.UpdateCachedCount(Entry.Id, posts.Count);
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[SavedSearchRowViewModel] Error fetching previews for query '{EffectiveQuery}': {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
