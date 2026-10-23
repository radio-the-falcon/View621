using System;

namespace View621.Services;

/// <summary>
/// Service tracking the user's e621 favourite post count and broadcasting changes to UI badges.
/// </summary>
public class FavoritesService
{
    private static FavoritesService? _instance;
    public static FavoritesService Instance => _instance ??= new FavoritesService();

    public event EventHandler? FavoritesChanged;

    private int? _cachedCount;
    public int? CachedCount => _cachedCount;

    public void SetCount(int count)
    {
        _cachedCount = Math.Max(0, count);
        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Increment()
    {
        if (_cachedCount.HasValue)
        {
            _cachedCount++;
        }
        else
        {
            _cachedCount = 1;
        }
        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Decrement()
    {
        if (_cachedCount.HasValue)
        {
            _cachedCount = Math.Max(0, _cachedCount.Value - 1);
        }
        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Invalidate()
    {
        _cachedCount = null;
        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }
}
