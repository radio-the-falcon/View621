using System.Threading;
using System.Threading.Tasks;
using E6Viewer.Models;

namespace E6Viewer.Services;

/// <summary>
/// Service interface for downloading, caching, and serving offline sample images.
/// </summary>
public interface IImageCacheService
{
    /// <summary>
    /// Gets the path to a cached image on disk for the given post ID if available.
    /// </summary>
    string? GetCachedImagePath(long postId);

    /// <summary>
    /// Downloads the sample (or preview) image of a post to the local cache folder.
    /// Returns the local file path on success, or null on failure.
    /// </summary>
    Task<string?> CacheSampleImageAsync(ImagePost post, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the cached image for the given post ID from the cache folder.
    /// </summary>
    void DeleteCachedImage(long postId);

    /// <summary>
    /// Clears all cached images from the cache folder.
    /// </summary>
    void ClearAllCachedImages();
}
