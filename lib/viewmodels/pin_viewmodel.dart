import 'package:flutter/foundation.dart';
import '../models/image_post.dart';
import '../services/bookmark_service.dart';
import '../services/e621_api_client.dart';
import '../services/post_cache_service.dart';

class PinViewModel extends ChangeNotifier {
  final E621ApiClient _apiClient = E621ApiClient();
  final List<ImagePost> _posts = [];
  bool _isLoading = false;
  bool _isDisposed = false;
  bool get isDisposed => _isDisposed;

  PinViewModel() {
    BookmarkService.instance.addListener(_onBookmarksChanged);
    loadBookmarkedPosts();
  }

  List<ImagePost> get posts => List.unmodifiable(_posts);
  bool get isLoading => _isLoading;
  int get count => _posts.length;

  @override
  void notifyListeners() {
    if (!_isDisposed) {
      super.notifyListeners();
    }
  }

  void _onBookmarksChanged() {
    loadBookmarkedPosts();
  }

  Future<void> loadBookmarkedPosts() async {
    if (_isDisposed) return;
    _isLoading = true;
    notifyListeners();

    final ids = BookmarkService.instance.bookmarkedPostIds;
    final loaded = <ImagePost>[];

    for (final id in ids) {
      if (_isDisposed) return;
      var post = PostCacheService.instance.getCachedPost(id);
      post ??= await _apiClient.getPostById(id);
      if (_isDisposed) return;
      if (post != null) {
        post.isBookmarked = true;
        loaded.add(post);
      }
    }

    if (!_isDisposed) {
      _posts.clear();
      _posts.addAll(loaded);
      _isLoading = false;
      notifyListeners();
    }
  }

  void clearAll() {
    if (_isDisposed) return;
    BookmarkService.instance.clearAllBookmarks();
  }

  @override
  void dispose() {
    _isDisposed = true;
    BookmarkService.instance.removeListener(_onBookmarksChanged);
    super.dispose();
  }
}
