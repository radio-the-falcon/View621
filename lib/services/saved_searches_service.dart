import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import '../models/saved_search_entry.dart';
import 'storage_locations.dart';

/// Persists and manages user saved searches in ~/.View621/saved_searches.json.
class SavedSearchesService extends ChangeNotifier {
  static SavedSearchesService? _instance;
  static SavedSearchesService get instance => _instance ??= SavedSearchesService._();

  final List<SavedSearchEntry> _savedSearches = [];

  SavedSearchesService._() {
    StorageLocations.ensureDirectories();
    _loadSavedSearches();
  }

  List<SavedSearchEntry> get savedSearches => List.unmodifiable(_savedSearches);

  bool isSaved(String effectiveQuery) {
    if (effectiveQuery.trim().isEmpty) return false;
    final trimmed = effectiveQuery.trim().toLowerCase();
    return _savedSearches.any((s) => s.effectiveQuery.trim().toLowerCase() == trimmed);
  }

  void saveSearch(
    String displayTitle,
    String effectiveQuery, {
    String? sortKey,
    bool isSortReversed = false,
    int? cachedCount,
  }) {
    if (effectiveQuery.trim().isEmpty) return;
    final trimmedEffective = effectiveQuery.trim();
    final trimmedDisplay = displayTitle.trim().isEmpty ? trimmedEffective : displayTitle.trim();

    _savedSearches.removeWhere((s) => s.effectiveQuery.trim().toLowerCase() == trimmedEffective.toLowerCase());

    final entry = SavedSearchEntry(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      displayTitle: trimmedDisplay,
      effectiveQuery: trimmedEffective,
      sortKey: sortKey,
      isSortReversed: isSortReversed,
      cachedCount: cachedCount,
      createdAt: DateTime.now().toUtc(),
    );

    _savedSearches.insert(0, entry);
    _saveToDisk();
    notifyListeners();
  }

  void removeSavedSearch(String idOrEffectiveQuery) {
    if (idOrEffectiveQuery.trim().isEmpty) return;
    final trimmed = idOrEffectiveQuery.trim().toLowerCase();

    final countBefore = _savedSearches.length;
    _savedSearches.removeWhere(
      (s) => s.id.toLowerCase() == trimmed || s.effectiveQuery.trim().toLowerCase() == trimmed,
    );

    if (_savedSearches.length != countBefore) {
      _saveToDisk();
      notifyListeners();
    }
  }

  void updateCachedCount(String effectiveQuery, int count) {
    final trimmed = effectiveQuery.trim().toLowerCase();
    bool updated = false;
    for (final s in _savedSearches) {
      if (s.effectiveQuery.trim().toLowerCase() == trimmed) {
        s.cachedCount = count;
        updated = true;
      }
    }
    if (updated) {
      _saveToDisk();
      notifyListeners();
    }
  }

  void clearAllSavedSearches() {
    if (_savedSearches.isEmpty) return;
    _savedSearches.clear();
    _saveToDisk();
    notifyListeners();
  }

  void _loadSavedSearches() {
    try {
      final file = File(StorageLocations.savedSearchesFilePath);
      if (file.existsSync()) {
        final content = file.readAsStringSync();
        if (content.trim().isNotEmpty) {
          final list = jsonDecode(content);
          if (list is List) {
            _savedSearches.clear();
            for (final item in list) {
              if (item is Map<String, dynamic>) {
                _savedSearches.add(SavedSearchEntry.fromJson(item));
              }
            }
          }
        }
      }
    } catch (e) {
      StorageLocations.log('[SavedSearchesService Load Error] $e');
    }
  }

  void _saveToDisk() {
    try {
      StorageLocations.ensureDirectories();
      final file = File(StorageLocations.savedSearchesFilePath);
      final jsonList = _savedSearches.map((s) => s.toJson()).toList();
      final jsonStr = const JsonEncoder.withIndent('  ').convert(jsonList);
      file.writeAsStringSync(jsonStr);
    } catch (e) {
      StorageLocations.log('[SavedSearchesService Save Error] $e');
    }
  }
}
