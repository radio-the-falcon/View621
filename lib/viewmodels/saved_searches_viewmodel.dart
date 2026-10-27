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

  SavedSearchesViewModel() {
    SavedSearchesService.instance.addListener(_onSavedSearchesChanged);
    _syncRows();
  }

  List<SavedSearchRow> get rows => List.unmodifiable(_rows);
  bool get isLoading => _isLoading;

  void _onSavedSearchesChanged() {
    _syncRows();
  }

  void _syncRows() {
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
    row.isLoading = true;
    notifyListeners();

    try {
      final posts = await _apiClient.searchPosts(row.entry.effectiveQuery, page: 1, limit: 4);
      row.previewPosts = posts;

      if (row.entry.cachedCount == null) {
        final count = await _apiClient.getPostCount(row.entry.effectiveQuery);
        if (count != null) {
          row.entry.cachedCount = count;
          SavedSearchesService.instance.updateCachedCount(row.entry.effectiveQuery, count);
        }
      }
    } catch (_) {}

    row.isLoading = false;
    notifyListeners();
  }

  Future<void> refreshAll() async {
    _isLoading = true;
    notifyListeners();

    for (final row in _rows) {
      await _fetchRowPreviews(row);
    }

    _isLoading = false;
    notifyListeners();
  }

  void removeSavedSearch(String id) {
    SavedSearchesService.instance.removeSavedSearch(id);
  }

  @override
  void dispose() {
    SavedSearchesService.instance.removeListener(_onSavedSearchesChanged);
    super.dispose();
  }
}
