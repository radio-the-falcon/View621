using System;
using System.IO;

namespace View621.Services;

/// <summary>
/// Centralised paths and directory helpers for View621 data storage in ~/.View621.
/// </summary>
public static class StorageLocations
{
    public static string HomeDirectory
    {
        get
        {
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(profile)) return profile;

            var userProfileEnv = Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrWhiteSpace(userProfileEnv)) return userProfileEnv;

            var homeEnv = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrWhiteSpace(homeEnv)) return homeEnv;

            return AppContext.BaseDirectory;
        }
    }

    /// <summary>
    /// Root datastore directory: ~/.View621
    /// </summary>
    public static string DataDirectory => Path.Combine(HomeDirectory, ".View621");

    /// <summary>
    /// Legacy datastore directory ~/.umamiff for backwards compatibility migration.
    /// </summary>
    public static string LegacyUmamiffDirectory => Path.Combine(HomeDirectory, ".umamiff");

    /// <summary>
    /// Bookmarks datastore file: ~/.View621/bookmarks.json
    /// </summary>
    public static string BookmarksFilePath => Path.Combine(DataDirectory, "bookmarks.json");

    /// <summary>
    /// Application settings file: ~/.View621/settings.json
    /// </summary>
    public static string SettingsFilePath => Path.Combine(DataDirectory, "settings.json");

    /// <summary>
    /// Previous search history file: ~/.View621/search_history.json
    /// </summary>
    public static string SearchHistoryFilePath => Path.Combine(DataDirectory, "search_history.json");

    /// <summary>
    /// Saved searches datastore file: ~/.View621/saved_searches.json
    /// </summary>
    public static string SavedSearchesFilePath => Path.Combine(DataDirectory, "saved_searches.json");

    /// <summary>
    /// Cached items directory: ~/.View621/cache
    /// </summary>
    public static string CacheDirectory => Path.Combine(DataDirectory, "cache");

    /// <summary>
    /// Cached sample images directory for offline access: ~/.View621/cache/images
    /// </summary>
    public static string ImageCacheDirectory => Path.Combine(CacheDirectory, "images");

    /// <summary>
    /// Cached posts metadata: ~/.View621/cache/posts.json
    /// </summary>
    public static string CachedPostsFilePath => Path.Combine(CacheDirectory, "posts.json");

    /// <summary>
    /// Application diagnostic log file: ~/.View621/app.log
    /// </summary>
    public static string LogFilePath => Path.Combine(DataDirectory, "app.log");

    /// <summary>
    /// Legacy settings path in %LocalAppData%\E6Viewer\settings.json for backwards compatibility migration.
    /// </summary>
    public static string LegacySettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "E6Viewer",
        "settings.json");

    /// <summary>
    /// Returns the local cached image file path for the post if it exists on disk, or null otherwise.
    /// </summary>
    public static string? GetExistingCachedImagePath(long postId)
    {
        if (postId <= 0) return null;
        try
        {
            if (!Directory.Exists(ImageCacheDirectory)) return null;
            var pattern = $"{postId}.*";
            var files = Directory.GetFiles(ImageCacheDirectory, pattern);
            return files.Length > 0 ? files[0] : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Computes the target cached image path for a post.
    /// </summary>
    public static string GetTargetCachedImagePath(long postId, string? extension)
    {
        var ext = extension?.TrimStart('.') ?? "jpg";
        if (string.IsNullOrWhiteSpace(ext)) ext = "jpg";
        return Path.Combine(ImageCacheDirectory, $"{postId}.{ext}");
    }

    /// <summary>
    /// Ensures that ~/.View621, ~/.View621/cache, and ~/.View621/cache/images directories exist,
    /// migrating any files from legacy ~/.umamiff if present.
    /// </summary>
    public static void EnsureDirectories()
    {
        try
        {
            if (!Directory.Exists(DataDirectory))
            {
                if (Directory.Exists(LegacyUmamiffDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(DataDirectory);
                        foreach (var file in Directory.GetFiles(LegacyUmamiffDirectory))
                        {
                            var dest = Path.Combine(DataDirectory, Path.GetFileName(file));
                            if (!File.Exists(dest))
                            {
                                File.Copy(file, dest, overwrite: false);
                            }
                        }
                    }
                    catch
                    {
                        // Fallback
                    }
                }
                else
                {
                    Directory.CreateDirectory(DataDirectory);
                }
            }
            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }
            if (!Directory.Exists(ImageCacheDirectory))
            {
                Directory.CreateDirectory(ImageCacheDirectory);
            }
        }
        catch
        {
            // Best effort directory creation
        }
    }

    /// <summary>
    /// Writes diagnostic messages to ~/.View621/app.log.
    /// </summary>
    public static void Log(string message)
    {
        try
        {
            EnsureDirectories();
            var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}\n";
            File.AppendAllText(LogFilePath, entry);
        }
        catch
        {
            // Best effort logging
        }
    }
}
