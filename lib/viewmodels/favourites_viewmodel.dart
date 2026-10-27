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

  FavouritesViewModel() {
    loadFavorites();
  }

  List<ImagePost> get posts => List.unmodifiable(_posts);
  bool get isLoading => _isLoading;
  String get statusMessage => _statusMessage;
  bool get isLoggedIn => AppSettings.instance.isLoggedIn;

  Future<void> loadFavorites() async {
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
      _posts.clear();
      _posts.addAll(results);

      final totalCount = await _apiClient.getFavoritesCount();
      if (totalCount != null) {
        FavoritesService.instance.setCount(totalCount);
      } else {
        FavoritesService.instance.setCount(results.length);
      }

      _statusMessage = _posts.isEmpty ? 'No favourited posts found.' : '${_posts.length} favourited posts';
    } catch (e) {
      _statusMessage = 'Error loading favourites: $e';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
