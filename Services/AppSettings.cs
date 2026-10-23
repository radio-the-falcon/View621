using System;
using System.IO;
using System.Text;
using System.Text.Json;
using View621.Helpers;

namespace View621.Services;

/// <summary>
/// Application settings and persistent preferences.
/// </summary>
public static class AppSettings
{
    public static event EventHandler? SettingsChanged;
    public static event EventHandler? FilterSettingsChanged;
    public static event EventHandler? LoginStateChanged;
    public static event EventHandler? SearchHistoryChanged
    {
        add => SearchHistoryService.Instance.HistoryChanged += value;
        remove => SearchHistoryService.Instance.HistoryChanged -= value;
    }

    private static readonly string SettingsFilePath = StorageLocations.SettingsFilePath;

    private static string _apiBaseUrl = "https://e621.net"; // Always e621.net
    private static int _pageSize = 40;
    private static string _username = string.Empty;
    private static string _apiKey = string.Empty;

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
                FilterSettingsChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    public static string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                SaveSettings();
                SettingsChanged?.Invoke(null, EventArgs.Empty);
                LoginStateChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    public static string ApiKey
    {
        get => _apiKey;
        set
        {
            if (_apiKey != value)
            {
                _apiKey = value;
                SaveSettings();
                SettingsChanged?.Invoke(null, EventArgs.Empty);
                LoginStateChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Indicates whether credentials (username and API key) are present.
    /// </summary>
    public static bool IsLoggedIn => !string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_apiKey);

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
                FilterSettingsChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Backward compatibility alias for ShowGender
    /// </summary>
    public static string ShowSex
    {
        get => ShowGender;
        set => ShowGender = value;
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
                FilterSettingsChanged?.Invoke(null, EventArgs.Empty);
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

    public static System.Collections.Generic.IReadOnlyList<string> SearchHistory
    {
        get
        {
            var list = SearchHistoryService.Instance.GetHistory();
            if (list.Count > 0) return list;
            return _searchHistory.AsReadOnly();
        }
    }

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

    private static string? EncryptSecret(string? plainText) => DpapiHelper.Encrypt(plainText);

    private static string? DecryptSecret(string? cipherText) => DpapiHelper.Decrypt(cipherText);

    private class SettingsData
    {
        public string? ApiBaseUrl { get; set; }
        public int PageSize { get; set; } = 40;
        public string? Username { get; set; }
        public string? EncryptedApiKey { get; set; }
        public string? ApiKey { get; set; } // Backward compatibility migration
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

            var legacyUmamiffSettings = Path.Combine(StorageLocations.LegacyUmamiffDirectory, "settings.json");
            var filePathToLoad = File.Exists(SettingsFilePath)
                ? SettingsFilePath
                : (File.Exists(legacyUmamiffSettings)
                    ? legacyUmamiffSettings
                    : (File.Exists(StorageLocations.LegacySettingsFilePath) ? StorageLocations.LegacySettingsFilePath : null));

            var needsMigrationSave = false;

            if (filePathToLoad != null)
            {
                var json = File.ReadAllText(filePathToLoad);
                var data = JsonSerializer.Deserialize<SettingsData>(json);
                if (data != null)
                {
                    _apiBaseUrl = "https://e621.net"; // Always e621.net
                    if (data.PageSize > 0) _pageSize = data.PageSize;
                    if (data.Username != null) _username = data.Username;

                    // Decrypt API key via Windows DPAPI, or migrate from legacy plain-text
                    if (!string.IsNullOrWhiteSpace(data.EncryptedApiKey))
                    {
                        var decrypted = DecryptSecret(data.EncryptedApiKey);
                        if (decrypted != null)
                        {
                            _apiKey = decrypted;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(data.ApiKey))
                    {
                        _apiKey = data.ApiKey;
                        needsMigrationSave = true; // Migrate plain-text key to DPAPI encrypted format
                    }

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

            // Also check standalone search_history.json in ~/.View621 or legacy ~/.umamiff if present
            var historyPath = File.Exists(StorageLocations.SearchHistoryFilePath)
                ? StorageLocations.SearchHistoryFilePath
                : (File.Exists(Path.Combine(StorageLocations.LegacyUmamiffDirectory, "search_history.json"))
                    ? Path.Combine(StorageLocations.LegacyUmamiffDirectory, "search_history.json")
                    : null);

            if (historyPath != null)
            {
                try
                {
                    var historyJson = File.ReadAllText(historyPath);
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

            // If we migrated from legacy path or need to encrypt legacy plain-text key, immediately save
            if (filePathToLoad != SettingsFilePath || needsMigrationSave)
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
                Username = _username,
                EncryptedApiKey = EncryptSecret(_apiKey),
                ApiKey = null, // Never persist in plain text!
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

            // Also persist previous searches in ~/.View621/search_history.json
            var historyJson = JsonSerializer.Serialize(_searchHistory, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StorageLocations.SearchHistoryFilePath, historyJson);
        }
        catch
        {
            // Best effort persistence
        }
    }
}
