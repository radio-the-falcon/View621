using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace E6Viewer.Services;

/// <summary>
/// Persists and manages recent search terms in ~/.umamiff/search_history.json.
/// </summary>
public class SearchHistoryService : ISearchHistoryService
{
    private static ISearchHistoryService? _instance;
    public static ISearchHistoryService Instance => _instance ??= new SearchHistoryService();

    public static void Initialize(ISearchHistoryService service)
    {
        _instance = service;
    }

    public event EventHandler? HistoryChanged;

    private readonly object _lock = new();
    private readonly List<string> _history = new();
    private const int MaxHistoryItems = 100;

    public SearchHistoryService()
    {
        StorageLocations.EnsureDirectories();
        LoadHistory();
    }

    public IReadOnlyList<string> GetHistory()
    {
        lock (_lock)
        {
            return _history.ToList();
        }
    }

    public void AddSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        var trimmed = query.Trim();

        lock (_lock)
        {
            // Remove existing duplicate (case-insensitive) so the new query moves to top
            _history.RemoveAll(q => string.Equals(q, trimmed, StringComparison.OrdinalIgnoreCase));
            _history.Insert(0, trimmed);

            if (_history.Count > MaxHistoryItems)
            {
                _history.RemoveRange(MaxHistoryItems, _history.Count - MaxHistoryItems);
            }
        }

        SaveHistory();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        var trimmed = query.Trim();

        bool changed = false;
        lock (_lock)
        {
            changed = _history.RemoveAll(q => string.Equals(q, trimmed, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        if (changed)
        {
            SaveHistory();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            if (_history.Count == 0) return;
            _history.Clear();
        }

        SaveHistory();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadHistory()
    {
        lock (_lock)
        {
            _history.Clear();
            try
            {
                var filePath = StorageLocations.SearchHistoryFilePath;
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var items = JsonSerializer.Deserialize<List<string>>(json);
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            if (!string.IsNullOrWhiteSpace(item) && !_history.Contains(item.Trim(), StringComparer.OrdinalIgnoreCase))
                            {
                                _history.Add(item.Trim());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StorageLocations.Log($"[SearchHistory Load Error] {ex}");
            }
        }
    }

    private void SaveHistory()
    {
        lock (_lock)
        {
            try
            {
                StorageLocations.EnsureDirectories();
                var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(StorageLocations.SearchHistoryFilePath, json);
            }
            catch (Exception ex)
            {
                StorageLocations.Log($"[SearchHistory Save Error] {ex}");
            }
        }
    }
}
