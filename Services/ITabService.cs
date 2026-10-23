using System;
using View621.Models;

namespace View621.Services;

public interface ITabService
{
    event Action? ExploreRequested;
    void NavigateToExplore();
    void OpenSearchInNewTab(string? query = null);
    void OpenPostInNewTab(ImagePost post, PostNavigationContext? context = null);
    void OpenPoolInNewTab(long poolId, string? poolName = null);
    void CloseCurrentTab();
}

