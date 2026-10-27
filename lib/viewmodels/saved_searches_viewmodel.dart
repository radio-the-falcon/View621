import 'package:flutter/foundation.dart';
import '../models/image_post.dart';
import '../models/saved_search_entry.dart';
import '../services/e621_api_client.dart';
import '../services/saved_searches_service.dart';

class SavedSearchRow {
  final SavedSearchEntry entry;
  List<ImagePost> previewPosts = [];
  bool isLoading = false;

  SavedSearchRow({required this.entry});
}

class SavedSearchesViewModel extends ChangeNotifier {
  final E621ApiClient _apiClient = E621ApiClient();
  final List<SavedSearchRow> _rows = [];
  bool _isLoading = false;
  bool _isDisposed = false;
  bool get isDisposed => _isDisposed;

  SavedSearchesViewModel() {
    SavedSearchesService.instance.addListener(_onSavedSearchesChanged);
    _syncRows();
  }

  List<SavedSearchRow> get rows => List.unmodifiable(_rows);
  bool get isLoading => _isLoading;

  @override
  void notifyListeners() {
    if (!_isDisposed) {
      super.notifyListeners();
    }
  }

  void _onSavedSearchesChanged() {
    _syncRows();
  }

  void _syncRows() {
    if (_isDisposed) return;
    final entries = SavedSearchesService.instance.savedSearches;
    final existingMap = {for (final r in _rows) r.entry.id: r};

    _rows.clear();
    for (final entry in entries) {
      if (existingMap.containsKey(entry.id)) {
        _rows.add(existingMap[entry.id]!);
      } else {
        final row = SavedSearchRow(entry: entry);
        _rows.add(row);
        _fetchRowPreviews(row);
      }
    }
    notifyListeners();
  }

  Future<void> _fetchRowPreviews(SavedSearchRow row) async {
    if (_isDisposed) return;
    row.isLoading = true;
    notifyListeners();

    try {
      final posts = await _apiClient.searchPosts(row.entry.effectiveQuery, page: 1, limit: 4);
      if (_isDisposed) return;
      row.previewPosts = posts;

      if (row.entry.cachedCount == null) {
        final count = await _apiClient.getPostCount(row.entry.effectiveQuery);
        if (_isDisposed) return;
        if (count != null) {
          row.entry.cachedCount = count;
          SavedSearchesService.instance.updateCachedCount(row.entry.effectiveQuery, count);
        }
      }
    } catch (_) {}

    if (!_isDisposed) {
      row.isLoading = false;
      notifyListeners();
    }
  }

  Future<void> refreshAll() async {
    if (_isDisposed) return;
    _isLoading = true;
    notifyListeners();

    for (final row in _rows) {
      if (_isDisposed) return;
      await _fetchRowPreviews(row);
    }

    if (!_isDisposed) {
      _isLoading = false;
      notifyListeners();
    }
  }

  void removeSavedSearch(String id) {
    if (_isDisposed) return;
    SavedSearchesService.instance.removeSavedSearch(id);
  }

  @override
  void dispose() {
    _isDisposed = true;
    SavedSearchesService.instance.removeListener(_onSavedSearchesChanged);
    super.dispose();
  }
}
