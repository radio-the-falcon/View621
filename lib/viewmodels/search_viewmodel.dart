import 'dart:async';
import 'package:flutter/foundation.dart';
import '../models/image_post.dart';
import '../services/app_settings.dart';
import '../services/bookmark_service.dart';
import '../services/e621_api_client.dart';
import '../services/search_history_service.dart';

class SortOption {
  final String key;
  final String normalLabel;
  final String reversedLabel;
  final String normalTerm;
  final String reversedTerm;

  const SortOption({
    required this.key,
    required this.normalLabel,
    required this.reversedLabel,
    required this.normalTerm,
    required this.reversedTerm,
  });
}

class SearchViewModel extends ChangeNotifier {
  final E621ApiClient _apiClient = E621ApiClient();

  String _searchQuery = '';
  bool _isLoading = false;
  String _statusMessage = '';
  int _currentPage = 1;
  bool _hasNextPage = false;
  bool _hasPreviousPage = false;

  String _sortKey = 'id';
  bool _isSortReversed = false;

  final List<ImagePost> _posts = [];
  final List<String> _tagSuggestions = [];
  Timer? _suggestDebounce;

  static const List<SortOption> sortOptions = [
    SortOption(
      key: 'id',
      normalLabel: 'Default (Newest ID first)',
      reversedLabel: 'Oldest ID first',
      normalTerm: 'order:id_desc',
      reversedTerm: 'order:id_asc',
    ),
    SortOption(
      key: 'created',
      normalLabel: 'Date created (Newest first)',
      reversedLabel: 'Date created (Oldest first)',
      normalTerm: 'order:created',
      reversedTerm: 'order:created_asc',
    ),
    SortOption(
      key: 'score',
      normalLabel: 'Score (Highest first)',
      reversedLabel: 'Score (Lowest first)',
      normalTerm: 'order:score',
      reversedTerm: 'order:score_asc',
    ),
    SortOption(
      key: 'favcount',
      normalLabel: 'Favourites (Most first)',
      reversedLabel: 'Favourites (Fewest first)',
      normalTerm: 'order:favcount',
      reversedTerm: 'order:favcount_asc',
    ),
  ];

  SearchViewModel({String? initialQuery}) {
    _sortKey = AppSettings.instance.lastSortKey;
    _isSortReversed = AppSettings.instance.isSortReversed;

    if (initialQuery != null && initialQuery.isNotEmpty) {
      _searchQuery = initialQuery;
      _extractSortTokens();
    }

    AppSettings.instance.addListener(_onSettingsChanged);
    BookmarkService.instance.addListener(syncBookmarkStates);

    search(resetPage: true);
  }

  String get searchQuery => _searchQuery;
  set searchQuery(String val) {
    if (_searchQuery != val) {
      _searchQuery = val;
      notifyListeners();
    }
  }

  bool get isLoading => _isLoading;
  String get statusMessage => _statusMessage;
  int get currentPage => _currentPage;
  bool get hasNextPage => _hasNextPage;
  bool get hasPreviousPage => _hasPreviousPage;
  String get sortKey => _sortKey;
  bool get isSortReversed => _isSortReversed;
  List<ImagePost> get posts => List.unmodifiable(_posts);
  List<String> get tagSuggestions => List.unmodifiable(_tagSuggestions);

  String get sortLabel {
    final opt = sortOptions.firstWhere((o) => o.key == _sortKey, orElse: () => sortOptions.first);
    return _isSortReversed ? opt.reversedLabel : opt.normalLabel;
  }

  void _onSettingsChanged() {
    search(resetPage: true, forceRefresh: true);
  }

  void syncBookmarkStates() {
    for (final post in _posts) {
      post.isBookmarked = BookmarkService.instance.isBookmarked(post.id);
    }
    notifyListeners();
  }

  bool _tryApplySortToken(String token) {
    for (final opt in sortOptions) {
      if (token.toLowerCase() == opt.normalTerm.toLowerCase()) {
        _sortKey = opt.key;
        _isSortReversed = false;
        return true;
      }
      if (token.toLowerCase() == opt.reversedTerm.toLowerCase()) {
        _sortKey = opt.key;
        _isSortReversed = true;
        return true;
      }
    }
    return false;
  }

  void _extractSortTokens() {
    final kept = <String>[];
    for (final token in _searchQuery.split(' ')) {
      if (token.trim().isEmpty) continue;
      if (!_tryApplySortToken(token.trim())) {
        kept.add(token.trim());
      }
    }
    _searchQuery = kept.join(' ');
  }

  String buildEffectiveQuery() {
    final terms = <String>[];

    if (_searchQuery.trim().isNotEmpty) {
      terms.add(_searchQuery.trim());
    }

    final opt = sortOptions.firstWhere((o) => o.key == _sortKey, orElse: () => sortOptions.first);
    final sortTerm = _isSortReversed ? opt.reversedTerm : opt.normalTerm;
    terms.add(sortTerm);

    final gender = AppSettings.instance.showGender.toLowerCase();
    if (gender == 'male') {
      terms.add('-female');
    } else if (gender == 'female') {
      terms.add('-male');
    }

    final rating = AppSettings.instance.ratingFilter.toLowerCase();
    if (rating == 'safe') {
      terms.add('rating:s');
    } else if (rating == 'questionable') {
      terms.add('-rating:e');
    }

    return terms.join(' ');
  }

  void setSort(String key) {
    if (key.toLowerCase() == 'id' || key.isEmpty) {
      _sortKey = 'id';
      _isSortReversed = false;
    } else {
      _sortKey = key;
    }
    AppSettings.instance.lastSortKey = _sortKey;
    AppSettings.instance.isSortReversed = _isSortReversed;
    search(resetPage: true);
  }

  void setSortReversed(bool reversed) {
    _isSortReversed = reversed;
    AppSettings.instance.isSortReversed = reversed;
    search(resetPage: true);
  }

  Future<void> search({bool resetPage = true, bool forceRefresh = false}) async {
    if (resetPage) {
      _currentPage = 1;
    }

    _isLoading = true;
    _statusMessage = 'Loading images...';
    notifyListeners();

    if (_searchQuery.trim().isNotEmpty) {
      SearchHistoryService.instance.addSearch(_searchQuery.trim());
      AppSettings.instance.lastSearchQuery = _searchQuery.trim();
    }

    final query = buildEffectiveQuery();
    final limit = AppSettings.instance.pageSize;

    try {
      final results = await _apiClient.searchPosts(query, page: _currentPage, limit: limit);
      _posts.clear();
      _posts.addAll(results);

      _hasPreviousPage = _currentPage > 1;
      _hasNextPage = results.length >= limit;

      if (results.isEmpty) {
        _statusMessage = _currentPage == 1 ? 'No posts found.' : 'No more posts.';
      } else {
        _statusMessage = 'Page $_currentPage (${results.length} posts)';
      }
    } catch (e) {
      _statusMessage = 'Error loading posts: $e';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  void nextPage() {
    if (!_hasNextPage || _isLoading) return;
    _currentPage++;
    search(resetPage: false);
  }

  void previousPage() {
    if (!_hasPreviousPage || _isLoading || _currentPage <= 1) return;
    _currentPage--;
    search(resetPage: false);
  }

  void onQueryChanged(String query) {
    searchQuery = query;

    _suggestDebounce?.cancel();
    _suggestDebounce = Timer(const Duration(milliseconds: 250), () async {
      final lastToken = query.trim().split(' ').lastWhere((t) => t.isNotEmpty, orElse: () => '');
      if (lastToken.length >= 2) {
        final suggestions = await _apiClient.autocompleteTags(lastToken);
        _tagSuggestions.clear();
        _tagSuggestions.addAll(suggestions);
        notifyListeners();
      } else {
        _tagSuggestions.clear();
        notifyListeners();
      }
    });
  }

  void selectSuggestion(String suggestion) {
    final tokens = _searchQuery.trim().split(' ');
    if (tokens.isNotEmpty) {
      tokens.removeLast();
    }
    tokens.add(suggestion);
    searchQuery = '${tokens.join(' ')} ';
    _tagSuggestions.clear();
    notifyListeners();
  }

  @override
  void dispose() {
    _suggestDebounce?.cancel();
    AppSettings.instance.removeListener(_onSettingsChanged);
    BookmarkService.instance.removeListener(syncBookmarkStates);
    super.dispose();
  }
}
