using System;
using System.IO;

namespace E6Viewer.Services;

/// <summary>
/// Centralised paths and directory helpers for View621 data storage in ~/.view621.
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
    /// Root datastore directory: ~/.view621
    /// </summary>
    public static string DataDirectory => Path.Combine(HomeDirectory, ".view621");

    /// <summary>
    /// Legacy datastore directory for migration: ~/.umamiff
    /// </summary>
    public static string LegacyUmamiffDirectory => Path.Combine(HomeDirectory, ".umamiff");

    /// <summary>
    /// Bookmarks datastore file: ~/.view621/bookmarks.json
    /// </summary>
    public static string BookmarksFilePath => Path.Combine(DataDirectory, "bookmarks.json");

    /// <summary>
    /// Application settings file: ~/.view621/settings.json
    /// </summary>
    public static string SettingsFilePath => Path.Combine(DataDirectory, "settings.json");

    /// <summary>
    /// Previous search history file: ~/.view621/search_history.json
    /// </summary>
    public static string SearchHistoryFilePath => Path.Combine(DataDirectory, "search_history.json");

    /// <summary>
    /// Saved searches datastore file: ~/.view621/saved_searches.json
    /// </summary>
    public static string SavedSearchesFilePath => Path.Combine(DataDirectory, "saved_searches.json");

    /// <summary>
    /// Cached items directory: ~/.view621/cache
    /// </summary>
    public static string CacheDirectory => Path.Combine(DataDirectory, "cache");

    /// <summary>
    /// Cached sample images directory for offline access: ~/.view621/cache/images
    /// </summary>
    public static string ImageCacheDirectory => Path.Combine(CacheDirectory, "images");

    /// <summary>
    /// Cached posts metadata: ~/.view621/cache/posts.json
    /// </summary>
    public static string CachedPostsFilePath => Path.Combine(CacheDirectory, "posts.json");

    /// <summary>
    /// Application diagnostic log file: ~/.view621/app.log
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
    /// Ensures that ~/.view621, ~/.view621/cache, and ~/.view621/cache/images directories exist,
    /// and migrates existing files from ~/.umamiff if present.
    /// </summary>
    public static void EnsureDirectories()
    {
        try
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            // Automatic migration from ~/.umamiff if ~/.view621 is newly created
            MigrateLegacyData();

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

    private static void MigrateLegacyData()
    {
        try
        {
            if (!Directory.Exists(LegacyUmamiffDirectory)) return;

            string[] filesToMigrate = ["bookmarks.json", "settings.json", "search_history.json", "saved_searches.json"];
            foreach (var file in filesToMigrate)
            {
                var legacyFile = Path.Combine(LegacyUmamiffDirectory, file);
                var targetFile = Path.Combine(DataDirectory, file);
                if (File.Exists(legacyFile) && !File.Exists(targetFile))
                {
                    File.Copy(legacyFile, targetFile, overwrite: false);
                }
            }
        }
        catch
        {
            // Best effort migration
        }
    }

    /// <summary>
    /// Writes diagnostic messages to ~/.view621/app.log.
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
