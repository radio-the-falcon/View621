using System;
using E6Viewer.Models;

namespace E6Viewer.Services;

public class TabService : ITabService
{
    private static ITabService? _instance;
    public static ITabService Instance => _instance ??= new TabService();

    public static void Initialize(ITabService service)
    {
        _instance = service;
    }

    public event Action? ExploreRequested;
    public event Action<string?>? SearchTabRequested;
    public event Action<ImagePost, PostNavigationContext?>? PostTabRequested;
    public event Action<long, string?>? PoolTabRequested;
    public event Action? CloseCurrentTabRequested;

    public void NavigateToExplore()
    {
        ExploreRequested?.Invoke();
    }

    public void OpenSearchInNewTab(string? query = null)
    {
        SearchTabRequested?.Invoke(query);
    }

    public void OpenPostInNewTab(ImagePost post, PostNavigationContext? context = null)
    {
        PostTabRequested?.Invoke(post, context);
    }

    public void OpenPoolInNewTab(long poolId, string? poolName = null)
    {
        PoolTabRequested?.Invoke(poolId, poolName);
    }

    public void CloseCurrentTab()
    {
        CloseCurrentTabRequested?.Invoke();
    }
}
