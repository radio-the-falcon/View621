using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using View621.Models;
using View621.Services;

namespace View621.ViewModels;

/// <summary>
/// View model managing all saved search rows in SavedSearchesView.
/// </summary>
public class SavedSearchesViewModel : INotifyPropertyChanged
{
    private readonly ISavedSearchesService _savedSearchesService;
    private readonly ISearchCacheService _searchCacheService;
    private readonly IImageService _apiClient;

    public ObservableCollection<SavedSearchRowViewModel> Rows { get; } = new();

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing != value)
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _hasNoSavedSearches;
    public bool HasNoSavedSearches
    {
        get => _hasNoSavedSearches;
        set
        {
            if (_hasNoSavedSearches != value)
            {
                _hasNoSavedSearches = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SavedSearchesViewModel(
        ISavedSearchesService? savedSearchesService = null,
        ISearchCacheService? searchCacheService = null,
        IImageService? apiClient = null)
    {
        _savedSearchesService = savedSearchesService ?? SavedSearchesService.Instance;
        _searchCacheService = searchCacheService ?? SearchCacheService.Instance;
        _apiClient = apiClient ?? new E621ApiClient();

        _savedSearchesService.SavedSearchesChanged += OnSavedSearchesChanged;
    }

    private void OnSavedSearchesChanged(object? sender, EventArgs e)
    {
        ReloadRows();
    }

    /// <summary>
    /// Loads rows from the saved searches datastore and initiates previews.
    /// </summary>
    public void ReloadRows()
    {
        var savedList = _savedSearchesService.GetSavedSearches();
        HasNoSavedSearches = savedList.Count == 0;

        // Synchronise Rows collection with savedList
        var existingMap = Rows.ToDictionary(r => r.Entry.Id);
        var newRows = new List<SavedSearchRowViewModel>();

        foreach (var entry in savedList)
        {
            if (existingMap.TryGetValue(entry.Id, out var existingRow))
            {
                existingRow.CachedCount = entry.CachedCount;
                newRows.Add(existingRow);
            }
            else
            {
                var newRow = new SavedSearchRowViewModel(entry, _savedSearchesService, _searchCacheService, _apiClient);
                newRows.Add(newRow);
                _ = newRow.LoadPreviewAsync(forceRefresh: false);
            }
        }

        Rows.Clear();
        foreach (var row in newRows)
        {
            Rows.Add(row);
        }
    }

    /// <summary>
    /// Refreshes all saved searches: bypasses cache, fetches the latest 8 preview posts, and updates cached counts.
    /// </summary>
    public async Task RefreshAllAsync()
    {
        if (IsRefreshing) return;
        IsRefreshing = true;

        try
        {
            var tasks = Rows.Select(r => r.LoadPreviewAsync(forceRefresh: true)).ToList();
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[SavedSearchesViewModel] Error refreshing all saved searches: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Deletes a saved search.
    /// </summary>
    public void DeleteSavedSearch(SavedSearchRowViewModel row)
    {
        if (row == null) return;
        _savedSearchesService.RemoveSavedSearch(row.Entry.Id);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
