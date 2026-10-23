using System.Collections.Generic;
using View621.Models;

namespace View621.Services;

/// <summary>
/// Service responsible for caching and retrieving post metadata in memory and persisting to disk.
/// </summary>
public interface IPostCacheService
{
    /// <summary>
    /// Gets a cached post by ID if available.
    /// </summary>
    ImagePost? GetCachedPost(long postId);

    /// <summary>
    /// Checks whether a post exists in the cache.
    /// </summary>
    bool TryGetPost(long postId, out ImagePost? post);

    /// <summary>
    /// Stores or updates a post in the cache and persists to disk.
    /// </summary>
    void CachePost(ImagePost post);

    /// <summary>
    /// Stores or updates multiple posts in the cache and persists to disk.
    /// </summary>
    void CachePosts(IEnumerable<ImagePost> posts);

    /// <summary>
    /// Returns all cached posts.
    /// </summary>
    IReadOnlyList<ImagePost> GetAllCachedPosts();

    /// <summary>
    /// Removes a post from the cache if present.
    /// </summary>
    void RemoveCachedPost(long postId);
}
