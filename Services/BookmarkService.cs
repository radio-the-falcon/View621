using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using E6Viewer.Models;

namespace E6Viewer.Services;

/// <summary>
/// Manages bookmarked posts and persists post IDs to ~/.umamiff in a JSON file.
/// </summary>
public class BookmarkService : IBookmarkService
{
    private static IBookmarkService? _instance;
    public static IBookmarkService Instance => _instance ??= new BookmarkService();

    public static void Initialize(IBookmarkService service)
    {
        _instance = service;
    }

    public event EventHandler? BookmarksChanged;

    private readonly object _lock = new();
    private readonly List<long> _bookmarkedIds = new();
    private readonly HashSet<long> _bookmarkedIdSet = new();
    private readonly IPostCacheService _postCacheService;
    private readonly IImageService _imageService;

    public BookmarkService() : this(new E621ApiClient(), PostCacheService.Instance)
    {
    }

    public BookmarkService(IImageService imageService) : this(imageService, PostCacheService.Instance)
    {
    }

    public BookmarkService(IImageService imageService, IPostCacheService postCacheService)
    {
        _imageService = imageService;
        _postCacheService = postCacheService;
        StorageLocations.EnsureDirectories();
        LoadBookmarks();
    }

    public static string GetHomeDirectory() => StorageLocations.HomeDirectory;

    public static string GetDatastoreDirectory() => StorageLocations.DataDirectory;

    public static string GetDatastoreFilePath()
    {
        var defaultPath = StorageLocations.BookmarksFilePath;
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        try
        {
            var dir = StorageLocations.DataDirectory;
            if (Directory.Exists(dir))
            {
                var existingJson = Directory.GetFiles(dir, "*.json").FirstOrDefault();
                if (existingJson != null)
                {
                    return existingJson;
                }
            }
        }
        catch
        {
            // Ignore error searching directory
        }

        return defaultPath;
    }

    public bool IsBookmarked(long postId)
    {
        lock (_lock)
        {
            return _bookmarkedIdSet.Contains(postId);
        }
    }

    public bool ToggleBookmark(long postId, ImagePost? post = null)
    {
        if (postId <= 0) return false;

        bool isNowBookmarked;
        lock (_lock)
        {
            if (_bookmarkedIdSet.Contains(postId))
            {
                _bookmarkedIdSet.Remove(postId);
                _bookmarkedIds.Remove(postId);
                isNowBookmarked = false;
            }
            else
            {
                _bookmarkedIdSet.Add(postId);
                _bookmarkedIds.Add(postId);
                if (post != null)
                {
                    _postCacheService.CachePost(post);
                }
                isNowBookmarked = true;
            }
        }

        if (post != null)
        {
            post.IsBookmarked = isNowBookmarked;
            _postCacheService.CachePost(post);
        }

        SaveBookmarks();
        BookmarksChanged?.Invoke(this, EventArgs.Empty);

        if (isNowBookmarked)
        {
            if (post != null)
            {
                _ = ImageCacheService.Instance.CacheSampleImageAsync(post);
            }
            else if (_postCacheService.TryGetPost(postId, out var cached) && cached != null)
            {
                _ = ImageCacheService.Instance.CacheSampleImageAsync(cached);
            }
        }
        else
        {
            ImageCacheService.Instance.DeleteCachedImage(postId);
        }

        return isNowBookmarked;
    }

    public void AddBookmark(long postId, ImagePost? post = null)
    {
        if (postId <= 0) return;

        bool changed = false;
        lock (_lock)
        {
            if (_bookmarkedIdSet.Add(postId))
            {
                _bookmarkedIds.Add(postId);
                changed = true;
            }
            if (post != null)
            {
                _postCacheService.CachePost(post);
            }
        }

        if (post != null)
        {
            post.IsBookmarked = true;
            _postCacheService.CachePost(post);
        }

        if (changed)
        {
            SaveBookmarks();
            BookmarksChanged?.Invoke(this, EventArgs.Empty);

            if (post != null)
            {
                _ = ImageCacheService.Instance.CacheSampleImageAsync(post);
            }
            else if (_postCacheService.TryGetPost(postId, out var cached) && cached != null)
            {
                _ = ImageCacheService.Instance.CacheSampleImageAsync(cached);
            }
        }
    }

    public void RemoveBookmark(long postId)
    {
        if (postId <= 0) return;

        bool changed = false;
        lock (_lock)
        {
            if (_bookmarkedIdSet.Remove(postId))
            {
                _bookmarkedIds.Remove(postId);
                changed = true;
            }
        }

        if (_postCacheService.TryGetPost(postId, out var cachedPost) && cachedPost != null)
        {
            cachedPost.IsBookmarked = false;
        }

        ImageCacheService.Instance.DeleteCachedImage(postId);

        if (changed)
        {
            SaveBookmarks();
            BookmarksChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ClearAllBookmarks()
    {
        lock (_lock)
        {
            if (_bookmarkedIds.Count == 0) return;
            foreach (var id in _bookmarkedIds)
            {
                if (_postCacheService.TryGetPost(id, out var post) && post != null)
                {
                    post.IsBookmarked = false;
                }
            }
            _bookmarkedIds.Clear();
            _bookmarkedIdSet.Clear();
        }

        ImageCacheService.Instance.ClearAllCachedImages();
        SaveBookmarks();
        BookmarksChanged?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<long> GetBookmarkedPostIds()
    {
        lock (_lock)
        {
            return _bookmarkedIds.ToList();
        }
    }

    public void CachePost(ImagePost post)
    {
        _postCacheService.CachePost(post);
    }

    public ImagePost? GetCachedPost(long postId)
    {
        return _postCacheService.GetCachedPost(postId);
    }

    public async Task<IReadOnlyList<ImagePost>> GetBookmarkedPostsAsync(CancellationToken cancellationToken = default)
    {
        List<long> targetIds;
        lock (_lock)
        {
            // Show newest bookmarks first
            targetIds = _bookmarkedIds.AsEnumerable().Reverse().ToList();
        }

        if (targetIds.Count == 0)
        {
            return Array.Empty<ImagePost>();
        }

        // Identify posts missing from cache
        var missingIds = targetIds.Where(id => _postCacheService.GetCachedPost(id) == null).ToList();

        // Fetch missing posts in batches using e621 id query (e.g. id:1,2,3)
        const int batchSize = 50;
        for (int i = 0; i < missingIds.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = missingIds.Skip(i).Take(batchSize).ToList();
            try
            {
                var query = $"id:{string.Join(",", batch)}";
                var fetchedPosts = await _imageService.SearchPostsAsync(query, page: 1, limit: batch.Count, cancellationToken: cancellationToken);
                foreach (var post in fetchedPosts)
                {
                    _postCacheService.CachePost(post);
                }
            }
            catch
            {
                // Fallback to individual fetches for missing IDs in this batch if bulk query encountered an error
                foreach (var id in batch)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    try
                    {
                        var singlePost = await _imageService.GetPostByIdAsync(id, cancellationToken);
                        if (singlePost != null)
                        {
                            _postCacheService.CachePost(singlePost);
                        }
                    }
                    catch
                    {
                        // Ignore individual post error
                    }
                }
            }
        }

        // Assemble result in the requested order
        var result = new List<ImagePost>();
        foreach (var id in targetIds)
        {
            if (_postCacheService.TryGetPost(id, out var post) && post != null)
            {
                post.IsBookmarked = true;
                result.Add(post);

                // If not yet cached locally, download sample image in background for offline viewing
                if (StorageLocations.GetExistingCachedImagePath(id) == null)
                {
                    _ = ImageCacheService.Instance.CacheSampleImageAsync(post);
                }
            }
            else
            {
                // Create minimal placeholder post if deleted or unavailable from provider
                var placeholder = new ImagePost
                {
                    Id = id,
                    Title = $"Post #{id}",
                    Artist = "Unavailable or removed",
                    Rating = "Safe",
                    IsBookmarked = true
                };
                result.Add(placeholder);
            }
        }

        return result;
    }

    private void LoadBookmarks()
    {
        lock (_lock)
        {
            _bookmarkedIds.Clear();
            _bookmarkedIdSet.Clear();

            try
            {
                var filePath = GetDatastoreFilePath();
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        using var doc = JsonDocument.Parse(json);
                        ParseJsonElement(doc.RootElement);
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText("startup_error.log", $"[Bookmarks Load Error] {ex}\n");
                }
                catch { }
            }
        }
    }

    private void ParseJsonElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out var id))
                {
                    AddIdInternal(id);
                }
                else if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("id", out var idProp) && idProp.TryGetInt64(out var objId))
                {
                    AddIdInternal(objId);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    ParseJsonElement(prop.Value);
                }
                else if (long.TryParse(prop.Name, out var idKey))
                {
                    AddIdInternal(idKey);
                }
            }
        }
    }

    private void AddIdInternal(long id)
    {
        if (id > 0 && _bookmarkedIdSet.Add(id))
        {
            _bookmarkedIds.Add(id);
        }
    }

    private void SaveBookmarks()
    {
        lock (_lock)
        {
            try
            {
                StorageLocations.EnsureDirectories();

                var filePath = GetDatastoreFilePath();
                var json = JsonSerializer.Serialize(_bookmarkedIds, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                try
                {
                    StorageLocations.Log($"[Bookmarks Save Error] {ex}");
                }
                catch { }
            }
        }
    }
}
