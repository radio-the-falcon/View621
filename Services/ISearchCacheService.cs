using System.Collections.Generic;
using E6Viewer.Models;

namespace E6Viewer.Services;

/// <summary>
/// Service responsible for caching search query results to minimise network API calls.
/// </summary>
public interface ISearchCacheService
{
    /// <summary>
    /// Attempts to retrieve cached posts for the given query, page, and limit.
    /// </summary>
    bool TryGetSearchResults(string query, int page, int limit, out IReadOnlyList<ImagePost>? results);

    /// <summary>
    /// Stores or overwrites cached posts for the given query, page, and limit.
    /// </summary>
    void SetSearchResults(string query, int page, int limit, IReadOnlyList<ImagePost> results);

    /// <summary>
    /// Invalidates cached results for a specific query across all pages.
    /// </summary>
    void InvalidateQuery(string query);

    /// <summary>
    /// Clears the entire search result cache.
    /// </summary>
    void ClearCache();
}
