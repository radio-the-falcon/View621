import 'dart:io';
import 'package:path/path.dart' as p;

/// Centralised paths and directory helpers for View621 data storage in ~/.View621.
class StorageLocations {
  static String get homeDirectory {
    final userProfile = Platform.environment['USERPROFILE'];
    if (userProfile != null && userProfile.isNotEmpty) return userProfile;

    final home = Platform.environment['HOME'];
    if (home != null && home.isNotEmpty) return home;

    return Directory.current.path;
  }

  /// Root datastore directory: ~/.View621
  static String get dataDirectory => p.join(homeDirectory, '.View621');

  /// Bookmarks datastore file: ~/.View621/bookmarks.json
  static String get bookmarksFilePath => p.join(dataDirectory, 'bookmarks.json');

  /// Application settings file: ~/.View621/settings.json
  static String get settingsFilePath => p.join(dataDirectory, 'settings.json');

  /// Previous search history file: ~/.View621/search_history.json
  static String get searchHistoryFilePath => p.join(dataDirectory, 'search_history.json');

  /// Saved searches datastore file: ~/.View621/saved_searches.json
  static String get savedSearchesFilePath => p.join(dataDirectory, 'saved_searches.json');

  /// Cached items directory: ~/.View621/cache
  static String get cacheDirectory => p.join(dataDirectory, 'cache');

  /// Cached sample images directory for offline access: ~/.View621/cache/images
  static String get imageCacheDirectory => p.join(cacheDirectory, 'images');

  /// Cached posts metadata: ~/.View621/cache/posts.json
  static String get cachedPostsFilePath => p.join(cacheDirectory, 'posts.json');

  /// Application diagnostic log file: ~/.View621/app.log
  static String get logFilePath => p.join(dataDirectory, 'app.log');

  /// Legacy datastore directory ~/.umamiff for backwards compatibility migration
  static String get legacyUmamiffDirectory => p.join(homeDirectory, '.umamiff');

  /// Ensures that all required application directories exist.
  static void ensureDirectories() {
    try {
      final dir = Directory(dataDirectory);
      if (!dir.existsSync()) {
        dir.createSync(recursive: true);
      }

      final cacheDir = Directory(cacheDirectory);
      if (!cacheDir.existsSync()) {
        cacheDir.createSync(recursive: true);
      }

      final imgDir = Directory(imageCacheDirectory);
      if (!imgDir.existsSync()) {
        imgDir.createSync(recursive: true);
      }
    } catch (e) {
      // Ignore directory creation errors if already present
    }
  }

  /// Logs a message to ~/.View621/app.log
  static void log(String message) {
    try {
      ensureDirectories();
      final now = DateTime.now().toUtc().toIso8601String();
      final line = '[$now] $message\n';
      File(logFilePath).writeAsStringSync(line, mode: FileMode.append, flush: true);
    } catch (_) {
      // Ignore logging failures
    }
  }

  /// Returns the local cached image file path for the post if it exists on disk, or null otherwise.
  static String? getExistingCachedImagePath(int postId) {
    if (postId <= 0) return null;
    try {
      final dir = Directory(imageCacheDirectory);
      if (!dir.existsSync()) return null;

      final prefix = '$postId.';
      for (final entity in dir.listSync()) {
        if (entity is File) {
          final filename = p.basename(entity.path);
          if (filename.startsWith(prefix)) {
            return entity.path;
          }
        }
      }
    } catch (_) {
      // Ignore disk errors
    }
    return null;
  }
}
