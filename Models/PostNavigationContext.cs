using System.Collections.Generic;

namespace E6Viewer.Models;

public enum NavigationMode
{
    SearchResults,
    Pool
}

public class PostNavigationContext
{
    public NavigationMode Mode { get; set; } = NavigationMode.SearchResults;

    /// <summary>
    /// Snapshot of posts in the current search page or result list.
    /// </summary>
    public List<ImagePost> Posts { get; set; } = new();

    /// <summary>
    /// Index of the current post within Posts.
    /// </summary>
    public int CurrentIndex { get; set; }

    /// <summary>
    /// Pool ID if viewing a series/pool.
    /// </summary>
    public long? PoolId { get; set; }

    /// <summary>
    /// Name or title of the pool/series.
    /// </summary>
    public string? PoolName { get; set; }

    /// <summary>
    /// Complete list of post IDs in the pool in sequential reading order.
    /// </summary>
    public List<long>? PoolPostIds { get; set; }
}
