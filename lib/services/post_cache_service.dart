import 'dart:convert';
import 'dart:io';
import '../models/image_post.dart';
import 'storage_locations.dart';

/// In-memory and disk metadata cache backed by ~/.View621/cache/posts.json.
class PostCacheService {
  static PostCacheService? _instance;
  static PostCacheService get instance => _instance ??= PostCacheService._();

  final Map<int, ImagePost> _cache = {};

  PostCacheService._() {
    StorageLocations.ensureDirectories();
    _loadCachedPosts();
  }

  ImagePost? getCachedPost(int postId) {
    if (postId <= 0) return null;
    return _cache[postId];
  }

  bool tryGetPost(int postId, List<ImagePost?> out) {
    if (postId <= 0) return false;
    final post = _cache[postId];
    if (post != null) {
      if (out.isNotEmpty) out[0] = post;
      return true;
    }
    return false;
  }

  void cachePost(ImagePost post) {
    if (post.id <= 0) return;
    _cache[post.id] = post;
    _saveCachedPosts();
  }

  void cachePosts(Iterable<ImagePost> posts) {
    bool added = false;
    for (final post in posts) {
      if (post.id > 0) {
        _cache[post.id] = post;
        added = true;
      }
    }
    if (added) {
      _saveCachedPosts();
    }
  }

  void _loadCachedPosts() {
    try {
      final file = File(StorageLocations.cachedPostsFilePath);
      if (file.existsSync()) {
        final content = file.readAsStringSync();
        if (content.trim().isNotEmpty) {
          final data = jsonDecode(content);
          if (data is List) {
            for (final item in data) {
              if (item is Map<String, dynamic>) {
                final post = ImagePost.fromJson(item);
                if (post.id > 0) {
                  _cache[post.id] = post;
                }
              }
            }
          } else if (data is Map<String, dynamic>) {
            for (final entry in data.entries) {
              if (entry.value is Map<String, dynamic>) {
                final post = ImagePost.fromJson(entry.value as Map<String, dynamic>);
                if (post.id > 0) {
                  _cache[post.id] = post;
                }
              }
            }
          }
        }
      }
    } catch (e) {
      StorageLocations.log('[PostCacheService Load Error] $e');
    }
  }

  void _saveCachedPosts() {
    try {
      StorageLocations.ensureDirectories();
      final file = File(StorageLocations.cachedPostsFilePath);
      final jsonList = _cache.values.map((p) => p.toJson()).toList();
      final jsonStr = const JsonEncoder.withIndent('  ').convert(jsonList);
      file.writeAsStringSync(jsonStr);
    } catch (e) {
      StorageLocations.log('[PostCacheService Save Error] $e');
    }
  }
}
