import 'package:flutter/foundation.dart';
import '../models/image_post.dart';
import '../services/bookmark_service.dart';
import '../services/e621_api_client.dart';
import '../services/post_cache_service.dart';

class PinViewModel extends ChangeNotifier {
  final E621ApiClient _apiClient = E621ApiClient();
  final List<ImagePost> _posts = [];
  bool _isLoading = false;

  PinViewModel() {
    BookmarkService.instance.addListener(_onBookmarksChanged);
    loadBookmarkedPosts();
  }

  List<ImagePost> get posts => List.unmodifiable(_posts);
  bool get isLoading => _isLoading;
  int get count => _posts.length;

  void _onBookmarksChanged() {
    loadBookmarkedPosts();
  }

  Future<void> loadBookmarkedPosts() async {
    _isLoading = true;
    notifyListeners();

    final ids = BookmarkService.instance.bookmarkedPostIds;
    final loaded = <ImagePost>[];

    for (final id in ids) {
      var post = PostCacheService.instance.getCachedPost(id);
      post ??= await _apiClient.getPostById(id);
      if (post != null) {
        post.isBookmarked = true;
        loaded.add(post);
      }
    }

    _posts.clear();
    _posts.addAll(loaded);
    _isLoading = false;
    notifyListeners();
  }

  void clearAll() {
    BookmarkService.instance.clearAllBookmarks();
  }

  @override
  void dispose() {
    BookmarkService.instance.removeListener(_onBookmarksChanged);
    super.dispose();
  }
}
