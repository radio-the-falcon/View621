using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using View621.Models;

namespace View621.Services;

public interface IBookmarkService
{
    event EventHandler? BookmarksChanged;
    bool IsBookmarked(long postId);
    bool ToggleBookmark(long postId, ImagePost? post = null);
    void AddBookmark(long postId, ImagePost? post = null);
    void RemoveBookmark(long postId);
    void ClearAllBookmarks();
    IReadOnlyList<long> GetBookmarkedPostIds();
    Task<IReadOnlyList<ImagePost>> GetBookmarkedPostsAsync(CancellationToken cancellationToken = default);
    void CachePost(ImagePost post);
    ImagePost? GetCachedPost(long postId);
}
