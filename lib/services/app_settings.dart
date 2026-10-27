import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import '../helpers/dpapi_helper.dart';
import 'search_history_service.dart';
import 'storage_locations.dart';

/// Application settings and persistent preferences.
class AppSettings extends ChangeNotifier {
  static AppSettings? _instance;
  static AppSettings get instance => _instance ??= AppSettings._();

  int _pageSize = 40;
  String _username = '';
  String _apiKey = '';
  String _showGender = 'All'; // 'All', 'Male', 'Female'
  String _ratingFilter = 'Explicit'; // 'Safe', 'Questionable', 'Explicit'
  String _themeMode = 'Default'; // 'Default', 'Light', 'Dark'
  String _backdropKind = 'Mica'; // 'Mica', 'Acrylic'
  double _backdropOpacity = 0.85; // 0.1 to 1.0
  String _lastSearchQuery = '';
  String _lastSortKey = 'id';
  bool _isSortReversed = false;

  AppSettings._() {
    loadSettings();
  }

  String get apiBaseUrl => 'https://e621.net';

  int get pageSize => _pageSize;
  set pageSize(int value) {
    if (_pageSize != value) {
      _pageSize = value;
      saveSettings();
      notifyListeners();
    }
  }

  String get username => _username;
  set username(String value) {
    if (_username != value) {
      _username = value;
      saveSettings();
      notifyListeners();
    }
  }

  String get apiKey => _apiKey;
  set apiKey(String value) {
    if (_apiKey != value) {
      _apiKey = value;
      saveSettings();
      notifyListeners();
    }
  }

  bool get isLoggedIn => _username.trim().isNotEmpty && _apiKey.trim().isNotEmpty;

  String get showGender => _showGender;
  set showGender(String value) {
    if (_showGender != value) {
      _showGender = value;
      saveSettings();
      notifyListeners();
    }
  }

  String get ratingFilter => _ratingFilter;
  set ratingFilter(String value) {
    if (_ratingFilter != value) {
      _ratingFilter = value;
      saveSettings();
      notifyListeners();
    }
  }

  String get themeMode => _themeMode;
  set themeMode(String value) {
    if (_themeMode != value) {
      _themeMode = value;
      saveSettings();
      notifyListeners();
    }
  }

  String get backdropKind => _backdropKind;
  set backdropKind(String value) {
    if (_backdropKind != value) {
      _backdropKind = value;
      saveSettings();
      notifyListeners();
    }
  }

  double get backdropOpacity => _backdropOpacity;
  set backdropOpacity(double value) {
    final clamped = value.clamp(0.1, 1.0);
    if ((_backdropOpacity - clamped).abs() > 0.001) {
      _backdropOpacity = clamped;
      saveSettings();
      notifyListeners();
    }
  }

  String get lastSearchQuery => _lastSearchQuery;
  set lastSearchQuery(String value) {
    if (_lastSearchQuery != value) {
      _lastSearchQuery = value;
      saveSettings();
      notifyListeners();
    }
  }

  String get lastSortKey => _lastSortKey;
  set lastSortKey(String value) {
    if (_lastSortKey != value) {
      _lastSortKey = value;
      saveSettings();
      notifyListeners();
    }
  }

  bool get isSortReversed => _isSortReversed;
  set isSortReversed(bool value) {
    if (_isSortReversed != value) {
      _isSortReversed = value;
      saveSettings();
      notifyListeners();
    }
  }

  void loadSettings() {
    try {
      StorageLocations.ensureDirectories();
      final file = File(StorageLocations.settingsFilePath);
      if (file.existsSync()) {
        final jsonStr = file.readAsStringSync();
        if (jsonStr.trim().isNotEmpty) {
          final data = jsonDecode(jsonStr);
          if (data is Map<String, dynamic>) {
            final ps = data['PageSize'] ?? data['page_size'];
            if (ps is num && ps > 0) _pageSize = ps.toInt();

            final u = data['Username'] ?? data['username'];
            if (u != null) _username = u.toString();

            final encKey = data['EncryptedApiKey'] ?? data['encrypted_api_key'];
            final plainKey = data['ApiKey'] ?? data['api_key'];

            if (encKey != null && encKey.toString().isNotEmpty) {
              final decrypted = DpapiHelper.decrypt(encKey.toString());
              if (decrypted != null) {
                _apiKey = decrypted;
              }
            } else if (plainKey != null && plainKey.toString().isNotEmpty) {
              _apiKey = plainKey.toString();
              // Auto-migrate to encrypted
              saveSettings();
            }

            final sg = data['ShowGender'] ?? data['show_gender'] ?? data['ShowSex'];
            if (sg != null) _showGender = sg.toString();

            final rf = data['RatingFilter'] ?? data['rating_filter'];
            if (rf != null) _ratingFilter = rf.toString();

            final tm = data['Theme'] ?? data['theme'] ?? data['ThemeMode'];
            if (tm != null) _themeMode = tm.toString();

            final bk = data['BackdropKind'] ?? data['backdrop_kind'];
            if (bk != null) _backdropKind = bk.toString();

            final bo = data['BackdropOpacity'] ?? data['backdrop_opacity'];
            if (bo is num) _backdropOpacity = bo.toDouble().clamp(0.1, 1.0);

            final lsq = data['LastSearchQuery'] ?? data['last_search_query'];
            if (lsq != null) _lastSearchQuery = lsq.toString();

            final lsk = data['LastSortKey'] ?? data['last_sort_key'];
            if (lsk != null) _lastSortKey = lsk.toString();

            final isr = data['IsSortReversed'] ?? data['is_sort_reversed'];
            if (isr is bool) _isSortReversed = isr;
          }
        }
      }
    } catch (e) {
      StorageLocations.log('[AppSettings Load Error] $e');
    }
  }

  void saveSettings() {
    try {
      StorageLocations.ensureDirectories();
      final file = File(StorageLocations.settingsFilePath);

      String? encryptedApiKey;
      if (_apiKey.isNotEmpty) {
        encryptedApiKey = DpapiHelper.encrypt(_apiKey);
      }

      final map = <String, dynamic>{
        'ApiBaseUrl': apiBaseUrl,
        'PageSize': _pageSize,
        'Username': _username,
        'EncryptedApiKey': encryptedApiKey,
        'ApiKey': null, // Never persist in plain text!
        'ShowGender': _showGender,
        'RatingFilter': _ratingFilter,
        'Theme': _themeMode,
        'BackdropKind': _backdropKind,
        'BackdropOpacity': _backdropOpacity,
        'LastSearchQuery': _lastSearchQuery,
        'LastSortKey': _lastSortKey,
        'IsSortReversed': _isSortReversed,
        'SearchHistory': SearchHistoryService.instance.history,
      };

      final jsonStr = const JsonEncoder.withIndent('  ').convert(map);
      file.writeAsStringSync(jsonStr);
    } catch (e) {
      StorageLocations.log('[AppSettings Save Error] $e');
    }
  }
}
