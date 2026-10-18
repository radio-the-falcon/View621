using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using E6Viewer.Models;

namespace E6Viewer.Services;

/// <summary>
/// Downloads and caches sample images in ~/.umamiff/cache/images for offline access.
/// </summary>
public class ImageCacheService : IImageCacheService
{
    private static IImageCacheService? _instance;
    public static IImageCacheService Instance => _instance ??= new ImageCacheService();

    public static void Initialize(IImageCacheService service)
    {
        _instance = service;
    }

    private static readonly HttpClient HttpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Umamiff/1.0 (by Antigravity on e621)");
        return client;
    }

    public string? GetCachedImagePath(long postId)
    {
        return StorageLocations.GetExistingCachedImagePath(postId);
    }

    public async Task<string?> CacheSampleImageAsync(ImagePost post, CancellationToken cancellationToken = default)
    {
        if (post == null || post.Id <= 0) return null;

        var existing = StorageLocations.GetExistingCachedImagePath(post.Id);
        if (existing != null)
        {
            post.LocalSamplePath = existing;
            return existing;
        }

        var downloadUrl = !string.IsNullOrWhiteSpace(post.SampleUrl)
            ? post.SampleUrl
            : (!string.IsNullOrWhiteSpace(post.PreviewUrl) ? post.PreviewUrl : post.FullUrl);

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            return null;
        }

        try
        {
            StorageLocations.EnsureDirectories();

            var ext = Path.GetExtension(downloadUrl);
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = string.IsNullOrWhiteSpace(post.FileExtension) ? ".jpg" : $".{post.FileExtension}";
            }

            var targetPath = StorageLocations.GetTargetCachedImagePath(post.Id, ext);
            var tempPath = targetPath + ".tmp";

            using var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                StorageLocations.Log($"[ImageCache] Failed to download sample for post #{post.Id} - HTTP {response.StatusCode}");
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Move(tempPath, targetPath);

            post.LocalSamplePath = targetPath;
            StorageLocations.Log($"[ImageCache] Successfully cached sample image for post #{post.Id} to {targetPath}");
            return targetPath;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[ImageCache Error] Error downloading post #{post.Id}: {ex.Message}");
            return null;
        }
    }

    public void DeleteCachedImage(long postId)
    {
        if (postId <= 0) return;
        try
        {
            var existing = StorageLocations.GetExistingCachedImagePath(postId);
            if (existing != null && File.Exists(existing))
            {
                File.Delete(existing);
                StorageLocations.Log($"[ImageCache] Deleted cached image for post #{postId}");
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[ImageCache Error] Error deleting cached image for post #{postId}: {ex.Message}");
        }
    }

    public void ClearAllCachedImages()
    {
        try
        {
            if (Directory.Exists(StorageLocations.ImageCacheDirectory))
            {
                var files = Directory.GetFiles(StorageLocations.ImageCacheDirectory);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
                StorageLocations.Log("[ImageCache] Cleared all cached images");
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[ImageCache Error] Error clearing cached images: {ex.Message}");
        }
    }
}
