using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using View621.Models;

namespace View621.Services;

/// <summary>
/// Thread-safe post metadata cache backed by ~/.View621/cache/posts.json.
/// </summary>
public class PostCacheService : IPostCacheService
{
    private static IPostCacheService? _instance;
    public static IPostCacheService Instance => _instance ??= new PostCacheService();

    public static void Initialize(IPostCacheService service)
    {
        _instance = service;
    }

    private readonly ConcurrentDictionary<long, ImagePost> _postCache = new();
    private readonly object _saveLock = new();

    public PostCacheService()
    {
        StorageLocations.EnsureDirectories();
        LoadCachedPosts();
    }

    public ImagePost? GetCachedPost(long postId)
    {
        if (postId <= 0) return null;
        return _postCache.TryGetValue(postId, out var post) ? post : null;
    }

    public bool TryGetPost(long postId, out ImagePost? post)
    {
        if (postId <= 0)
        {
            post = null;
            return false;
        }
        return _postCache.TryGetValue(postId, out post);
    }

    public void CachePost(ImagePost post)
    {
        if (post != null && post.Id > 0)
        {
            _postCache[post.Id] = post;
            SaveCachedPosts();
        }
    }

    public void CachePosts(IEnumerable<ImagePost> posts)
    {
        if (posts == null) return;

        bool added = false;
        foreach (var post in posts)
        {
            if (post != null && post.Id > 0)
            {
                _postCache[post.Id] = post;
                added = true;
            }
        }

        if (added)
        {
            SaveCachedPosts();
        }
    }

    public IReadOnlyList<ImagePost> GetAllCachedPosts()
    {
        return _postCache.Values.ToList();
    }

    public void RemoveCachedPost(long postId)
    {
        if (postId > 0 && _postCache.TryRemove(postId, out _))
        {
            SaveCachedPosts();
        }
    }

    private void LoadCachedPosts()
    {
        try
        {
            var filePath = StorageLocations.CachedPostsFilePath;
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var posts = JsonSerializer.Deserialize<List<ImagePost>>(json);
                if (posts != null)
                {
                    foreach (var post in posts)
                    {
                        if (post != null && post.Id > 0)
                        {
                            _postCache[post.Id] = post;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[CachedPosts Load Error] {ex}");
        }
    }

    private void SaveCachedPosts()
    {
        lock (_saveLock)
        {
            try
            {
                StorageLocations.EnsureDirectories();
                var posts = _postCache.Values.ToList();
                var json = JsonSerializer.Serialize(posts, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(StorageLocations.CachedPostsFilePath, json);
            }
            catch (Exception ex)
            {
                StorageLocations.Log($"[CachedPosts Save Error] {ex}");
            }
        }
    }
}
