using System;

namespace View621.Models;

/// <summary>
/// Represents a saved search query bookmarked by the user.
/// </summary>
public class SavedSearchEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// User-friendly clean search query for the UI title (e.g. "canine fox").
    /// </summary>
    public string DisplayTitle { get; set; } = string.Empty;

    /// <summary>
    /// Full effective query with order, rating, and gender filters (e.g. "canine fox order:score -female").
    /// </summary>
    public string EffectiveQuery { get; set; } = string.Empty;

    public string? SortKey { get; set; }
    public bool IsSortReversed { get; set; }

    /// <summary>
    /// Cached count of matching results for this search.
    /// </summary>
    public int? CachedCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
