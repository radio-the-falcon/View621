using System;
using System.Collections.Generic;

namespace View621.Services;

/// <summary>
/// Service managing previous searches history with persistence in ~/.View621/search_history.json.
/// </summary>
public interface ISearchHistoryService
{
    event EventHandler? HistoryChanged;

    IReadOnlyList<string> GetHistory();
    void AddSearch(string query);
    void RemoveSearch(string query);
    void ClearHistory();
}
