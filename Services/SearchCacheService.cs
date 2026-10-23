using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using View621.Models;

namespace View621.Services;

/// <summary>
/// In-memory cache for search query results to avoid redundant API requests.
/// </summary>
public class SearchCacheService : ISearchCacheService
{
    private static ISearchCacheService? _instance;
    public static ISearchCacheService Instance => _instance ??= new SearchCacheService();

    public static void Initialize(ISearchCacheService service)
    {
        _instance = service;
    }

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(15);

    private record CacheEntry(IReadOnlyList<ImagePost> Posts, DateTime CreatedAt);

    private static string BuildKey(string query, int page, int limit)
    {
        var normalizedQuery = query?.Trim().ToLowerInvariant() ?? string.Empty;
        return $"{normalizedQuery}|p={page}|l={limit}";
    }

    public bool TryGetSearchResults(string query, int page, int limit, out IReadOnlyList<ImagePost>? results)
    {
        var key = BuildKey(query, page, limit);
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow - entry.CreatedAt <= _defaultTtl)
            {
                results = entry.Posts;
                return true;
            }
            // Expired entry
            _cache.TryRemove(key, out _);
        }

        results = null;
        return false;
    }

    public void SetSearchResults(string query, int page, int limit, IReadOnlyList<ImagePost> results)
    {
        if (results == null) return;
        var key = BuildKey(query, page, limit);
        _cache[key] = new CacheEntry(results, DateTime.UtcNow);
    }

    public void InvalidateQuery(string query)
    {
        var prefix = (query?.Trim().ToLowerInvariant() ?? string.Empty) + "|";
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
