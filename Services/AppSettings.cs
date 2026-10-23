using System;
using System.IO;
using System.Text.Json;

namespace E6Viewer.Services;

/// <summary>
/// Application settings and persistent preferences.
/// </summary>
public static class AppSettings
{
    public static event EventHandler? SettingsChanged;
    public static event EventHandler? SearchHistoryChanged
    {
        add => SearchHistoryService.Instance.HistoryChanged += value;
        remove => SearchHistoryService.Instance.HistoryChanged -= value;
    }

    private static readonly string SettingsFilePath = StorageLocations.SettingsFilePath;

    private static string _apiBaseUrl = "https://e621.net"; // Always e621.net
    private static int _pageSize = 40;

    // Show gender filter: "All", "Male" (-female), "Female" (-male)
    private static string _showGender = "All";

    // Rating filter: "Safe" (rating:s), "Questionable" (-rating:e), "Explicit" (all)
    private static string _ratingFilter = "Explicit";

    // Backdrop material and translucency
    private static string _backdropKind = "Mica"; // "Mica" or "Acrylic"
    private static double _backdropOpacity = 0.85; // 0.1 to 1.0

    private static string _lastSearchQuery = string.Empty;
    private static string _lastSortKey = string.Empty;
    private static bool _isSortReversed;
    private static System.Collections.Generic.List<string> _searchHistory = new();

    static AppSettings()
    {
        LoadSettings();
    }

    public static string ApiBaseUrl
    {
        get => "https://e621.net";
        set
        {
            // Lock to e621.net
            _apiBaseUrl = "https://e621.net";
        }
    }

    public static int PageSize
    {
        get => _pageSize;
        set
        {
            if (_pageSize != value)
            {
                _pageSize = value;
                SaveSettings();
                SettingsChanged?.Invoke(null, EventArgs.Empty);
            }

        }
    }

    /// <summary>
    /// Show gender filter: "All", "Male" (appends -female), "Female" (appends -male)
    /// </summary>
    public static string ShowGender
    {
        get => _showGender;
        set
        {
            if (_showGender != value)
            {
                _showGender = value;
                SaveSettings();
                SettingsChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Rating filter: "Safe" (rating:s), "Questionable" (-rating:e), "Explicit" (all)
    /// </summary>
    public static string RatingFilter
    {
        get => _ratingFilter;
        set
        {
            if (_ratingFilter != value)
            {
                _ratingFilter = value;
                SaveSettings();
                SettingsChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    public static string BackdropKind
    {
        get => _backdropKind;
        set
        {
            if (_backdropKind != value)
            {
                _backdropKind = value;
                SaveSettings();
                SettingsChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    public static double BackdropOpacity
    {
        get => _backdropOpacity;
        set
        {
            if (Math.Abs(_backdropOpacity - value) > 0.001)
            {
                _backdropOpacity = Math.Clamp(value, 0.1, 1.0);
                SaveSettings();
                SettingsChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    public static string LastSearchQuery
    {
        get => _lastSearchQuery;
        set
        {
            if (_lastSearchQuery != value)
            {
                _lastSearchQuery = value;
                SaveSettings();
            }
        }
    }

    public static string LastSortKey
    {
        get => _lastSortKey;
        set
        {
            if (_lastSortKey != value)
            {
                _lastSortKey = value;
                SaveSettings();
            }
        }
    }

    public static bool IsSortReversed
    {
        get => _isSortReversed;
        set
        {
            if (_isSortReversed != value)
            {
                _isSortReversed = value;
                SaveSettings();
            }
        }
    }

    public static System.Collections.Generic.IReadOnlyList<string> SearchHistory => SearchHistoryService.Instance.GetHistory();

    public static void AddSearchToHistory(string query)
    {
        SearchHistoryService.Instance.AddSearch(query);
    }

    public static void RemoveSearchFromHistory(string query)
    {
        SearchHistoryService.Instance.RemoveSearch(query);
    }

    public static void ClearSearchHistory()
    {
        SearchHistoryService.Instance.ClearHistory();
    }

    private class SettingsData
    {
        public string? ApiBaseUrl { get; set; }
        public int PageSize { get; set; } = 40;
        public string? ShowGender { get; set; }
        public string? ShowSex { get; set; } // Backward compatibility
        public string? RatingFilter { get; set; }
        public string? BackdropKind { get; set; }
        public double BackdropOpacity { get; set; } = 0.85;
        public string? LastSearchQuery { get; set; }
        public string? LastSortKey { get; set; }
        public bool IsSortReversed { get; set; }
        public System.Collections.Generic.List<string>? SearchHistory { get; set; }
    }

    private static void LoadSettings()
    {
        try
        {
            StorageLocations.EnsureDirectories();

            var filePathToLoad = File.Exists(SettingsFilePath)
                ? SettingsFilePath
                : (File.Exists(StorageLocations.LegacySettingsFilePath) ? StorageLocations.LegacySettingsFilePath : null);

            if (filePathToLoad != null)
            {
                var json = File.ReadAllText(filePathToLoad);
                var data = JsonSerializer.Deserialize<SettingsData>(json);
                if (data != null)
                {
                    _apiBaseUrl = "https://e621.net"; // Always e621.net
                    if (data.PageSize > 0) _pageSize = data.PageSize;
                    if (!string.IsNullOrWhiteSpace(data.ShowGender)) _showGender = data.ShowGender;
                    else if (!string.IsNullOrWhiteSpace(data.ShowSex)) _showGender = data.ShowSex;
                    if (!string.IsNullOrWhiteSpace(data.RatingFilter)) _ratingFilter = data.RatingFilter;
                    if (!string.IsNullOrWhiteSpace(data.BackdropKind)) _backdropKind = data.BackdropKind;
                    if (data.BackdropOpacity >= 0.1 && data.BackdropOpacity <= 1.0) _backdropOpacity = data.BackdropOpacity;
                    if (data.LastSearchQuery != null) _lastSearchQuery = data.LastSearchQuery;
                    if (data.LastSortKey != null) _lastSortKey = data.LastSortKey;
                    _isSortReversed = data.IsSortReversed;
                    if (data.SearchHistory != null)
                    {
                        _searchHistory = new System.Collections.Generic.List<string>(data.SearchHistory);
                    }
                }
            }

            // Also check standalone search_history.json in ~/.umamiff if present
            if (File.Exists(StorageLocations.SearchHistoryFilePath))
            {
                try
                {
                    var historyJson = File.ReadAllText(StorageLocations.SearchHistoryFilePath);
                    var historyData = JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(historyJson);
                    if (historyData != null && historyData.Count > 0)
                    {
                        foreach (var query in historyData)
                        {
                            if (!string.IsNullOrWhiteSpace(query) && !_searchHistory.Contains(query, StringComparer.OrdinalIgnoreCase))
                            {
                                _searchHistory.Add(query.Trim());
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore search history load error
                }
            }

            // If we migrated from legacy path, immediately save to ~/.umamiff
            if (filePathToLoad == StorageLocations.LegacySettingsFilePath)
            {
                SaveSettings();
            }
        }
        catch
        {
            // Fallback to defaults on corrupt settings
        }
    }

    private static void SaveSettings()
    {
        try
        {
            StorageLocations.EnsureDirectories();

            var data = new SettingsData
            {
                ApiBaseUrl = _apiBaseUrl,
                PageSize = _pageSize,
                ShowGender = _showGender,
                RatingFilter = _ratingFilter,
                BackdropKind = _backdropKind,
                BackdropOpacity = _backdropOpacity,
                LastSearchQuery = _lastSearchQuery,
                LastSortKey = _lastSortKey,
                IsSortReversed = _isSortReversed,
                SearchHistory = _searchHistory,
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);

            // Also persist previous searches in ~/.umamiff/search_history.json
            var historyJson = JsonSerializer.Serialize(_searchHistory, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StorageLocations.SearchHistoryFilePath, historyJson);
        }
        catch
        {
            // Best effort persistence
        }
    }
}
