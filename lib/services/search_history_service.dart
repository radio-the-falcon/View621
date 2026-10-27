import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'storage_locations.dart';

/// Persists and manages recent search terms in ~/.View621/search_history.json.
class SearchHistoryService extends ChangeNotifier {
  static SearchHistoryService? _instance;
  static SearchHistoryService get instance => _instance ??= SearchHistoryService._();

  final List<String> _history = [];
  static const int _maxHistoryItems = 100;

  SearchHistoryService._() {
    StorageLocations.ensureDirectories();
    _loadHistory();
  }

  List<String> get history => List.unmodifiable(_history);

  void addSearch(String query) {
    final trimmed = query.trim();
    if (trimmed.isEmpty) return;

    _history.removeWhere((q) => q.toLowerCase() == trimmed.toLowerCase());
    _history.insert(0, trimmed);

    if (_history.length > _maxHistoryItems) {
      _history.removeRange(_maxHistoryItems, _history.length);
    }

    _saveHistory();
    notifyListeners();
  }

  void removeSearch(String query) {
    final trimmed = query.trim();
    final countBefore = _history.length;
    _history.removeWhere((q) => q.toLowerCase() == trimmed.toLowerCase());

    if (_history.length != countBefore) {
      _saveHistory();
      notifyListeners();
    }
  }

  void clearHistory() {
    if (_history.isEmpty) return;
    _history.clear();
    _saveHistory();
    notifyListeners();
  }

  void _loadHistory() {
    try {
      final file = File(StorageLocations.searchHistoryFilePath);
      if (file.existsSync()) {
        final content = file.readAsStringSync();
        if (content.trim().isNotEmpty) {
          final list = jsonDecode(content);
          if (list is List) {
            _history.clear();
            for (final item in list) {
              if (item != null && item.toString().trim().isNotEmpty) {
                _history.add(item.toString().trim());
              }
            }
          }
        }
      }
    } catch (e) {
      StorageLocations.log('[SearchHistoryService Load Error] $e');
    }
  }

  void _saveHistory() {
    try {
      StorageLocations.ensureDirectories();
      final file = File(StorageLocations.searchHistoryFilePath);
      final jsonStr = const JsonEncoder.withIndent('  ').convert(_history);
      file.writeAsStringSync(jsonStr);
    } catch (e) {
      StorageLocations.log('[SearchHistoryService Save Error] $e');
    }
  }
}
