using System;
using System.Collections.Generic;

namespace E6Viewer.Services;

/// <summary>
/// Service managing previous searches history with persistence in ~/.umamiff/search_history.json.
/// </summary>
public interface ISearchHistoryService
{
    event EventHandler? HistoryChanged;

    IReadOnlyList<string> GetHistory();
    void AddSearch(string query);
    void RemoveSearch(string query);
    void ClearHistory();
}
