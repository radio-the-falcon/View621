import 'package:flutter/foundation.dart';
import '../models/image_post.dart';
import '../services/app_settings.dart';
import '../services/e621_api_client.dart';
import '../services/favorites_service.dart';

class FavouritesViewModel extends ChangeNotifier {
  final E621ApiClient _apiClient = E621ApiClient();

  final List<ImagePost> _posts = [];
  bool _isLoading = false;
  String _statusMessage = '';
  bool _isDisposed = false;
  bool get isDisposed => _isDisposed;

  FavouritesViewModel() {
    loadFavorites();
  }

  List<ImagePost> get posts => List.unmodifiable(_posts);
  bool get isLoading => _isLoading;
  String get statusMessage => _statusMessage;
  bool get isLoggedIn => AppSettings.instance.isLoggedIn;

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

  Future<void> loadFavorites() async {
    if (_isDisposed) return;
    if (!isLoggedIn) {
      _posts.clear();
      notifyListeners();
      return;
    }

    _isLoading = true;
    _statusMessage = 'Loading your favourites from e621...';
    notifyListeners();

    try {
      final results = await _apiClient.getFavorites(page: 1, limit: AppSettings.instance.pageSize);
      if (_isDisposed) return;
      _posts.clear();
      _posts.addAll(results);

      final totalCount = await _apiClient.getFavoritesCount();
      if (_isDisposed) return;
      if (totalCount != null) {
        FavoritesService.instance.setCount(totalCount);
      } else {
        FavoritesService.instance.setCount(results.length);
      }

      _statusMessage = _posts.isEmpty ? 'No favourited posts found.' : '${_posts.length} favourited posts';
    } catch (e) {
      if (_isDisposed) return;
      _statusMessage = 'Error loading favourites: $e';
    } finally {
      if (!_isDisposed) {
        _isLoading = false;
        notifyListeners();
      }
    }
  }
}
