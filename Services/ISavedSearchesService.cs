using System;
using System.Collections.Generic;
using View621.Models;

namespace View621.Services;

/// <summary>
/// Service contract for persisting and managing user saved searches.
/// </summary>
public interface ISavedSearchesService
{
    event EventHandler? SavedSearchesChanged;

    IReadOnlyList<SavedSearchEntry> GetSavedSearches();

    bool IsSaved(string effectiveQuery);

    void SaveSearch(string displayTitle, string effectiveQuery, string? sortKey = null, bool isSortReversed = false, int? cachedCount = null);

    void RemoveSavedSearch(string idOrEffectiveQuery);

    void UpdateCachedCount(string id, int count);
}
