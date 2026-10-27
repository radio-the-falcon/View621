import 'package:flutter/foundation.dart';
import '../models/e621_dto.dart';
import '../models/image_post.dart';
import '../models/post_navigation_context.dart';
import '../services/app_settings.dart';
import '../services/bookmark_service.dart';
import '../services/e621_api_client.dart';
import '../services/favorites_service.dart';
import '../services/image_cache_service.dart';

class PostViewModel extends ChangeNotifier {
  final E621ApiClient _apiClient = E621ApiClient();

  PostNavigationContext context;
  ImagePost post;

  bool _isOriginalView = false;
  bool _isFitToFill = true;
  bool _isLoadingMedia = false;
  List<E621PoolDto> _pools = [];
  bool _isLoadingPools = false;
  bool _isDisposed = false;
  bool get isDisposed => _isDisposed;

  PostViewModel({required this.post, PostNavigationContext? navigationContext})
      : context = navigationContext ??
            PostNavigationContext(
              mode: PostNavigationMode.singlePost,
              posts: [post],
              currentIndex: 0,
            ) {
    _init();
  }

  @override
  void notifyListeners() {
    if (!_isDisposed) {
      super.notifyListeners();
    }
  }

  @override
  void dispose() {
    _isDisposed = true;
    super.dispose();
  }

  bool get isOriginalView => _isOriginalView;
  bool get isFitToFill => _isFitToFill;
  bool get isLoadingMedia => _isLoadingMedia;
  List<E621PoolDto> get pools => List.unmodifiable(_pools);
  bool get isLoadingPools => _isLoadingPools;

  bool get canGoPrevious => context.currentIndex > 0;
  bool get canGoNext {
    if (context.mode == PostNavigationMode.searchResults) {
      return context.currentIndex < context.posts.length - 1;
    } else if (context.mode == PostNavigationMode.pool) {
      final poolIds = context.poolPostIds;
      return poolIds != null && context.currentIndex < poolIds.length - 1;
    }
    return false;
  }

  void _init() {
    _loadRelationships();
    ImageCacheService.instance.cacheSampleImage(post);
  }

  void toggleResolution() {
    _isOriginalView = !_isOriginalView;
    notifyListeners();
  }

  void toggleFitToFill() {
    _isFitToFill = !_isFitToFill;
    notifyListeners();
  }

  void toggleBookmark() {
    final newState = BookmarkService.instance.toggleBookmark(post.id, post);
    post.isBookmarked = newState;
    notifyListeners();
  }

  Future<void> toggleFavorite() async {
    if (!AppSettings.instance.isLoggedIn) return;

    final wasFavorited = post.isFavorited;
    if (wasFavorited) {
      post.isFavorited = false;
      post.favCount = (post.favCount - 1).clamp(0, 9999999);
      FavoritesService.instance.decrement();
      notifyListeners();

      final success = await _apiClient.removeFavorite(post.id);
      if (!success) {
        post.isFavorited = true;
        post.favCount++;
        FavoritesService.instance.increment();
        notifyListeners();
      }
    } else {
      post.isFavorited = true;
      post.favCount++;
      FavoritesService.instance.increment();
      notifyListeners();

      final success = await _apiClient.addFavorite(post.id);
      if (!success) {
        post.isFavorited = false;
        post.favCount = (post.favCount - 1).clamp(0, 9999999);
        FavoritesService.instance.decrement();
        notifyListeners();
      }
    }
  }

  Future<void> vote(int score) async {
    if (!AppSettings.instance.isLoggedIn) return;

    // Toggle cancellation if already voted same score
    final targetScore = post.ourScore == score ? 0 : score;
    final result = await _apiClient.votePost(post.id, targetScore);

    if (result.success) {
      post.score = result.newScore;
      post.ourScore = result.ourScore;
      notifyListeners();
    }
  }

  Future<void> goToPrevious() async {
    if (!canGoPrevious) return;
    await _navigateToIndex(context.currentIndex - 1);
  }

  Future<void> goToNext() async {
    if (!canGoNext) return;
    await _navigateToIndex(context.currentIndex + 1);
  }

  Future<void> _navigateToIndex(int index) async {
    if (context.mode == PostNavigationMode.searchResults) {
      if (index >= 0 && index < context.posts.length) {
        context.currentIndex = index;
        post = context.posts[index];
        _isOriginalView = false;
        _init();
        notifyListeners();
      }
    } else if (context.mode == PostNavigationMode.pool) {
      final ids = context.poolPostIds;
      if (ids != null && index >= 0 && index < ids.length) {
        context.currentIndex = index;
        final targetId = ids[index];
        _isLoadingMedia = true;
        notifyListeners();

        final fetched = await _apiClient.getPostById(targetId);
        _isLoadingMedia = false;
        if (fetched != null) {
          post = fetched;
          _isOriginalView = false;
          _init();
        }
        notifyListeners();
      }
    }
  }

  Future<void> _loadRelationships() async {
    if (post.poolIds.isNotEmpty) {
      _isLoadingPools = true;
      notifyListeners();

      final list = <E621PoolDto>[];
      for (final id in post.poolIds) {
        final p = await _apiClient.getPoolById(id);
        if (p != null) list.add(p);
      }

      _pools = list;
      _isLoadingPools = false;
      notifyListeners();
    }
  }
}
