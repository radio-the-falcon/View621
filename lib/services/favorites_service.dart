import 'dart:math';
import 'package:flutter/foundation.dart';

/// Tracks the user's e621 favourite post count and broadcasts changes to UI badges.
class FavoritesService extends ChangeNotifier {
  static FavoritesService? _instance;
  static FavoritesService get instance => _instance ??= FavoritesService._();

  int? _cachedCount;
  int? get cachedCount => _cachedCount;

  FavoritesService._();

  void setCount(int count) {
    _cachedCount = max(0, count);
    notifyListeners();
  }

  void increment() {
    if (_cachedCount != null) {
      _cachedCount = _cachedCount! + 1;
    } else {
      _cachedCount = 1;
    }
    notifyListeners();
  }

  void decrement() {
    if (_cachedCount != null) {
      _cachedCount = max(0, _cachedCount! - 1);
    }
    notifyListeners();
  }

  void invalidate() {
    _cachedCount = null;
    notifyListeners();
  }
}
