using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using E6Viewer.Models;

namespace E6Viewer.Services;

public interface IImageService
{
    Task<IReadOnlyList<ImagePost>> SearchPostsAsync(string query, int page = 1, int limit = 40, CancellationToken cancellationToken = default);
    Task<ImagePost?> GetPostByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> AutocompleteTagsAsync(string query, CancellationToken cancellationToken = default);
    Task<E621PoolDto?> GetPoolByIdAsync(long poolId, CancellationToken cancellationToken = default);
    Task<int?> GetPostCountAsync(string query, CancellationToken cancellationToken = default);
}
