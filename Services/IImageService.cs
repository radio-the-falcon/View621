using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using View621.Models;

namespace View621.Services;

public interface IImageService
{
    Task<IReadOnlyList<ImagePost>> SearchPostsAsync(string query, int page = 1, int limit = 40, CancellationToken cancellationToken = default);
    Task<ImagePost?> GetPostByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> AutocompleteTagsAsync(string query, CancellationToken cancellationToken = default);
    Task<E621PoolDto?> GetPoolByIdAsync(long poolId, CancellationToken cancellationToken = default);
    Task<int?> GetPostCountAsync(string query, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> VerifyLoginAsync(string username, string apiKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImagePost>> GetFavoritesAsync(int page = 1, int limit = 40, CancellationToken cancellationToken = default);
    Task<int?> GetFavoritesCountAsync(CancellationToken cancellationToken = default);
    Task<bool> AddFavoriteAsync(long postId, CancellationToken cancellationToken = default);
    Task<bool> RemoveFavoriteAsync(long postId, CancellationToken cancellationToken = default);
    Task<(bool Success, int NewScore, int? OurScore)> VotePostAsync(long postId, int score, CancellationToken cancellationToken = default);
}
