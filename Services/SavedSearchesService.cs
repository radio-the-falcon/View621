using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using View621.Models;

namespace View621.Services;

/// <summary>
/// Persists and manages user saved searches in ~/.View621/saved_searches.json.
/// </summary>
public class SavedSearchesService : ISavedSearchesService
{
    private static ISavedSearchesService? _instance;
    public static ISavedSearchesService Instance => _instance ??= new SavedSearchesService();

    public static void Initialize(ISavedSearchesService service)
    {
        _instance = service;
    }

    public event EventHandler? SavedSearchesChanged;

    private readonly object _lock = new();
    private readonly List<SavedSearchEntry> _savedSearches = new();

    public SavedSearchesService()
    {
        StorageLocations.EnsureDirectories();
        LoadSavedSearches();
    }

    public IReadOnlyList<SavedSearchEntry> GetSavedSearches()
    {
        lock (_lock)
        {
            return _savedSearches.ToList();
        }
    }

    public bool IsSaved(string effectiveQuery)
    {
        if (string.IsNullOrWhiteSpace(effectiveQuery)) return false;
        var trimmed = effectiveQuery.Trim();

        lock (_lock)
        {
            return _savedSearches.Any(s => string.Equals(s.EffectiveQuery.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void SaveSearch(string displayTitle, string effectiveQuery, string? sortKey = null, bool isSortReversed = false, int? cachedCount = null)
    {
        if (string.IsNullOrWhiteSpace(effectiveQuery)) return;
        var trimmedEffective = effectiveQuery.Trim();
        var trimmedDisplay = string.IsNullOrWhiteSpace(displayTitle) ? trimmedEffective : displayTitle.Trim();

        lock (_lock)
        {
            // Remove existing duplicate with the same effective query so updated details come to the front
            _savedSearches.RemoveAll(s => string.Equals(s.EffectiveQuery.Trim(), trimmedEffective, StringComparison.OrdinalIgnoreCase));

            var entry = new SavedSearchEntry
            {
                DisplayTitle = trimmedDisplay,
                EffectiveQuery = trimmedEffective,
                SortKey = sortKey,
                IsSortReversed = isSortReversed,
                CachedCount = cachedCount,
                CreatedAt = DateTime.UtcNow
            };

            _savedSearches.Insert(0, entry);
        }

        SaveToDisk();
        SavedSearchesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveSavedSearch(string idOrEffectiveQuery)
    {
        if (string.IsNullOrWhiteSpace(idOrEffectiveQuery)) return;
        var trimmed = idOrEffectiveQuery.Trim();

        bool removed;
        lock (_lock)
        {
            var countBefore = _savedSearches.Count;
            _savedSearches.RemoveAll(s =>
                string.Equals(s.Id, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.EffectiveQuery.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
            removed = _savedSearches.Count != countBefore;
        }

        if (removed)
        {
            SaveToDisk();
            SavedSearchesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void UpdateCachedCount(string id, int count)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        bool updated = false;
        lock (_lock)
        {
            var entry = _savedSearches.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
            if (entry != null && entry.CachedCount != count)
            {
                entry.CachedCount = count;
                updated = true;
            }
        }

        if (updated)
        {
            SaveToDisk();
            SavedSearchesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void LoadSavedSearches()
    {
        try
        {
            var filePath = StorageLocations.SavedSearchesFilePath;
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var items = JsonSerializer.Deserialize<List<SavedSearchEntry>>(json);
                if (items != null)
                {
                    lock (_lock)
                    {
                        _savedSearches.Clear();
                        _savedSearches.AddRange(items);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[SavedSearchesService] Error loading saved searches: {ex.Message}");
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var filePath = StorageLocations.SavedSearchesFilePath;
            List<SavedSearchEntry> snapshot;
            lock (_lock)
            {
                snapshot = _savedSearches.ToList();
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(snapshot, options);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[SavedSearchesService] Error saving saved searches: {ex.Message}");
        }
    }
}
