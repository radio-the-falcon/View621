using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using E6Viewer.Models;
using E6Viewer.Services;

namespace E6Viewer.ViewModels;

/// <summary>
/// ViewModel managing pinned posts in PinView, designed to support future custom tagging and sorting.
/// </summary>
public partial class PinViewModel : ObservableObject
{
    private readonly IBookmarkService _bookmarkService;
    private CancellationTokenSource? _loadCts;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public ObservableCollection<ImagePost> Posts { get; } = new();

    public PinViewModel() : this(BookmarkService.Instance)
    {
    }

    public PinViewModel(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
        _bookmarkService.BookmarksChanged += BookmarkService_BookmarksChanged;
    }

    private void BookmarkService_BookmarksChanged(object? sender, EventArgs e)
    {
        var bookmarkedIdSet = new HashSet<long>(_bookmarkService.GetBookmarkedPostIds());
        for (int i = Posts.Count - 1; i >= 0; i--)
        {
            if (!bookmarkedIdSet.Contains(Posts[i].Id))
            {
                Posts.RemoveAt(i);
            }
        }
        UpdateStatusMessage();
    }

    public async Task LoadPinnedPostsAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        IsLoading = true;
        StatusMessage = "Loading pinned posts...";

        try
        {
            var results = await _bookmarkService.GetBookmarkedPostsAsync(ct);
            if (ct.IsCancellationRequested) return;

            Posts.Clear();
            foreach (var post in results)
            {
                Posts.Add(post);
            }

            UpdateStatusMessage();
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load pinned posts: {ex.Message}";
            StorageLocations.Log($"[PinViewModel Load Error] {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void CancelLoading()
    {
        _loadCts?.Cancel();
    }

    private void UpdateStatusMessage()
    {
        if (Posts.Count == 0)
        {
            StatusMessage = "No pinned posts yet.";
        }
        else
        {
            StatusMessage = $"{Posts.Count} {(Posts.Count == 1 ? "post" : "posts")} pinned";
        }
    }
}
