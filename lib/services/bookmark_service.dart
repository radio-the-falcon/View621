import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import '../models/image_post.dart';
import 'image_cache_service.dart';
import 'post_cache_service.dart';
import 'storage_locations.dart';

/// Manages bookmarked posts and persists post IDs to ~/.View621/bookmarks.json.
class BookmarkService extends ChangeNotifier {
  static BookmarkService? _instance;
  static BookmarkService get instance => _instance ??= BookmarkService._();

  final List<int> _bookmarkedIds = [];
  final Set<int> _bookmarkedIdSet = {};

  BookmarkService._() {
    StorageLocations.ensureDirectories();
    _loadBookmarks();
  }

  List<int> get bookmarkedPostIds => List.unmodifiable(_bookmarkedIds);

  bool isBookmarked(int postId) {
    return _bookmarkedIdSet.contains(postId);
  }

  bool toggleBookmark(int postId, [ImagePost? post]) {
    if (postId <= 0) return false;

    bool isNowBookmarked;
    if (_bookmarkedIdSet.contains(postId)) {
      _bookmarkedIdSet.remove(postId);
      _bookmarkedIds.remove(postId);
      isNowBookmarked = false;
      ImageCacheService.instance.deleteCachedImage(postId);
    } else {
      _bookmarkedIdSet.add(postId);
      _bookmarkedIds.add(postId);
      isNowBookmarked = true;

      if (post != null) {
        PostCacheService.instance.cachePost(post);
        ImageCacheService.instance.cacheSampleImage(post);
      } else {
        final cached = PostCacheService.instance.getCachedPost(postId);
        if (cached != null) {
          ImageCacheService.instance.cacheSampleImage(cached);
        }
      }
    }

    if (post != null) {
      post.isBookmarked = isNowBookmarked;
    }

    _saveBookmarks();
    notifyListeners();
    return isNowBookmarked;
  }

  void addBookmark(int postId, [ImagePost? post]) {
    if (postId <= 0) return;

    if (_bookmarkedIdSet.add(postId)) {
      _bookmarkedIds.add(postId);
      if (post != null) {
        post.isBookmarked = true;
        PostCacheService.instance.cachePost(post);
        ImageCacheService.instance.cacheSampleImage(post);
      }
      _saveBookmarks();
      notifyListeners();
    }
  }

  void removeBookmark(int postId) {
    if (postId <= 0) return;

    if (_bookmarkedIdSet.remove(postId)) {
      _bookmarkedIds.remove(postId);
      ImageCacheService.instance.deleteCachedImage(postId);
      _saveBookmarks();
      notifyListeners();
    }
  }

  void clearAllBookmarks() {
    if (_bookmarkedIds.isEmpty) return;
    _bookmarkedIds.clear();
    _bookmarkedIdSet.clear();
    ImageCacheService.instance.clearAllCachedImages();
    _saveBookmarks();
    notifyListeners();
  }

  void cachePost(ImagePost post) {
    PostCacheService.instance.cachePost(post);
  }

  ImagePost? getCachedPost(int postId) {
    return PostCacheService.instance.getCachedPost(postId);
  }

  void _loadBookmarks() {
    try {
      final file = File(StorageLocations.bookmarksFilePath);
      if (file.existsSync()) {
        final content = file.readAsStringSync();
        if (content.trim().isNotEmpty) {
          final data = jsonDecode(content);
          _bookmarkedIds.clear();
          _bookmarkedIdSet.clear();

          if (data is List) {
            for (final item in data) {
              if (item is num) {
                final id = item.toInt();
                if (id > 0 && _bookmarkedIdSet.add(id)) {
                  _bookmarkedIds.add(id);
                }
              } else if (item is Map && item.containsKey('id')) {
                final id = (item['id'] as num).toInt();
                if (id > 0 && _bookmarkedIdSet.add(id)) {
                  _bookmarkedIds.add(id);
                }
              }
            }
          }
        }
      }
    } catch (e) {
      StorageLocations.log('[BookmarkService Load Error] $e');
    }
  }

  void _saveBookmarks() {
    try {
      StorageLocations.ensureDirectories();
      final file = File(StorageLocations.bookmarksFilePath);
      final jsonStr = const JsonEncoder.withIndent('  ').convert(_bookmarkedIds);
      file.writeAsStringSync(jsonStr);
    } catch (e) {
      StorageLocations.log('[BookmarkService Save Error] $e');
    }
  }
}
