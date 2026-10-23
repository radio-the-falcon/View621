using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using View621.Models;

namespace View621.Services;

/// <summary>
/// Production API client for e621 / e926 following the OpenAPI specification.
/// </summary>
public class E621ApiClient : IImageService
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        // E621 / e926 API strictly requires a clean descriptive User-Agent header
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("View621/1.0 (by View621_user on e621)");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);

        // Apply Basic Auth if user provided API key in settings
        if (!string.IsNullOrWhiteSpace(AppSettings.Username) && !string.IsNullOrWhiteSpace(AppSettings.ApiKey))
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{AppSettings.Username}:{AppSettings.ApiKey}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        }

        return request;
    }

    public async Task<IReadOnlyList<ImagePost>> SearchPostsAsync(string query, int page = 1, int limit = 40, CancellationToken cancellationToken = default)
    {
        var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');
        var effectiveLimit = Math.Clamp(limit, 1, 320);
        var effectivePage = Math.Clamp(page, 1, 750);

        var urlBuilder = new StringBuilder($"{baseUrl}/posts.json?limit={effectiveLimit}&page={effectivePage}");
        if (!string.IsNullOrWhiteSpace(query))
        {
            urlBuilder.Append($"&tags={Uri.EscapeDataString(query.Trim())}");
        }

        using var request = CreateRequest(HttpMethod.Get, urlBuilder.ToString());
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<E621PostsResponse>(stream, cancellationToken: cancellationToken);

        if (result?.Posts == null)
        {
            return Array.Empty<ImagePost>();
        }

        return result.Posts.Select(MapToImagePost).ToList();
    }

    public async Task<ImagePost?> GetPostByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return null;

        var cached = BookmarkService.Instance.GetCachedPost(id);
        if (cached != null)
        {
            return cached;
        }

        var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/posts/{id}.json";

        using var request = CreateRequest(HttpMethod.Get, url);
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<E621SinglePostResponse>(stream, cancellationToken: cancellationToken);

        if (result?.Post != null)
        {
            var mappedPost = MapToImagePost(result.Post);
            BookmarkService.Instance.CachePost(mappedPost);
            return mappedPost;
        }

        return null;
    }

    public async Task<E621PoolDto?> GetPoolByIdAsync(long poolId, CancellationToken cancellationToken = default)
    {
        var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/pools/{poolId}.json";

        try
        {
            using var request = CreateRequest(HttpMethod.Get, url);
            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<E621PoolDto>(stream, cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> AutocompleteTagsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Array.Empty<string>();
        }

        var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/tags/autocomplete.json?search[name_matches]={Uri.EscapeDataString(query.Trim())}";

        try
        {
            using var request = CreateRequest(HttpMethod.Get, url);
            using var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var results = await JsonSerializer.DeserializeAsync<List<E621TagAutocompleteDto>>(stream, cancellationToken: cancellationToken);

            if (results == null)
            {
                return Array.Empty<string>();
            }

            return results
                .OrderByDescending(t => t.PostCount)
                .Select(t => t.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Take(12)
                .ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public async Task<int?> GetPostCountAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');
            var trimmedQuery = query?.Trim() ?? string.Empty;

            // Strategy 1: Check single tag metadata via /tags.json
            if (!string.IsNullOrWhiteSpace(trimmedQuery) && !trimmedQuery.Contains(' '))
            {
                var tagUrl = $"{baseUrl}/tags.json?search[name]={Uri.EscapeDataString(trimmedQuery)}";
                using var tagRequest = CreateRequest(HttpMethod.Get, tagUrl);
                using var tagResponse = await HttpClient.SendAsync(tagRequest, cancellationToken);
                if (tagResponse.IsSuccessStatusCode)
                {
                    await using var stream = await tagResponse.Content.ReadAsStreamAsync(cancellationToken);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                    {
                        var firstTag = doc.RootElement[0];
                        if (firstTag.TryGetProperty("post_count", out var countProp) && countProp.TryGetInt32(out var count))
                        {
                            return count;
                        }
                    }
                }
            }

            // Strategy 2: Scrape approximate/total count from the provider's /posts web HTML
            // Look specifically within the posts-info-count section or approximate-count span
            var webUrl = $"{baseUrl}/posts";
            if (!string.IsNullOrWhiteSpace(trimmedQuery))
            {
                webUrl += $"?tags={Uri.EscapeDataString(trimmedQuery)}";
            }

            using var webRequest = CreateRequest(HttpMethod.Get, webUrl);
            using var webResponse = await HttpClient.SendAsync(webRequest, cancellationToken);
            if (webResponse.IsSuccessStatusCode)
            {
                var html = await webResponse.Content.ReadAsStringAsync(cancellationToken);
                
                // e621 posts-info-count structure:
                // <span class="posts-info-count"><span class="approximate-count" ... data-count="631">
                var match = System.Text.RegularExpressions.Regex.Match(
                    html,
                    @"posts-info-count[^>]*>.*?data-count=""(\d+)""",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

                if (!match.Success)
                {
                    match = System.Text.RegularExpressions.Regex.Match(
                        html,
                        @"approximate-count[^>]*data-count=""(\d+)""",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                }

                if (match.Success && int.TryParse(match.Groups[1].Value, out var totalCount))
                {
                    return totalCount;
                }
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[E621ApiClient] Error retrieving post count for query '{query}': {ex.Message}");
        }

        return null;
    }

    private static ImagePost MapToImagePost(E621PostDto dto)
    {
        var artist = (dto.Tags?.Artist != null && dto.Tags.Artist.Count > 0)
            ? string.Join(", ", dto.Tags.Artist)
            : "Unknown";

        var rating = dto.Rating switch
        {
            "s" => "Safe",
            "q" => "Questionable",
            "e" => "Explicit",
            _ => dto.Rating
        };

        var ext = dto.File?.Ext?.ToLowerInvariant() ?? "jpg";
        var isImage = ext is "jpg" or "jpeg" or "png" or "webp" or "gif";

        // Thumbnail image preview (small size for fast gallery grid loading)
        var previewUrl = dto.Preview?.Url;
        if (string.IsNullOrWhiteSpace(previewUrl) && isImage)
        {
            previewUrl = dto.Sample?.Url ?? dto.File?.Url ?? string.Empty;
        }
        else if (string.IsNullOrWhiteSpace(previewUrl))
        {
            previewUrl = string.Empty;
        }

        // Reasonable size preview/sample image (typically ~850px-1280px) to prevent loading massive multi-megabyte files
        string sampleUrl;
        if (dto.Sample != null && dto.Sample.Has && !string.IsNullOrWhiteSpace(dto.Sample.Url))
        {
            sampleUrl = dto.Sample.Url;
        }
        else if (isImage && !string.IsNullOrWhiteSpace(dto.File?.Url))
        {
            sampleUrl = dto.File.Url;
        }
        else
        {
            sampleUrl = previewUrl;
        }

        var fullUrl = dto.File?.Url ?? sampleUrl;

        var childIds = dto.Relationships?.Children != null ? new List<long>(dto.Relationships.Children) : new List<long>();
        var poolIds = dto.Pools != null ? new List<long>(dto.Pools) : new List<long>();

        return new ImagePost
        {
            Id = dto.Id,
            Title = $"Post #{dto.Id}",
            Artist = artist,
            Rating = rating,
            Score = dto.Score?.Total ?? 0,
            FavCount = dto.FavCount,
            IsFavorited = dto.IsFavorited,
            Width = dto.File?.Width ?? dto.Sample?.Width ?? 0,
            Height = dto.File?.Height ?? dto.Sample?.Height ?? 0,
            FileExtension = dto.File?.Ext ?? "jpg",
            FileSizeFormatted = FormatBytes(dto.File?.Size ?? 0),
            CreatedAt = dto.CreatedAt,
            PreviewUrl = previewUrl,
            SampleUrl = sampleUrl,
            FullUrl = fullUrl,
            GeneralTags = dto.Tags?.General ?? new(),
            ArtistTags = dto.Tags?.Artist ?? new(),
            CharacterTags = dto.Tags?.Character ?? new(),
            SpeciesTags = dto.Tags?.Species ?? new(),
            CopyrightTags = dto.Tags?.Copyright ?? new(),
            Sources = dto.Sources ?? new(),
            Description = dto.Description ?? string.Empty,
            ParentId = dto.Relationships?.ParentId,
            HasChildren = dto.Relationships?.HasChildren ?? (childIds.Count > 0),
            ChildIds = childIds,
            PoolIds = poolIds
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 KB";
        string[] units = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < units.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.#} {units[order]}";
    }

    public async Task<(bool Success, string Message)> VerifyLoginAsync(string username, string apiKey, CancellationToken cancellationToken = default)
    {
        var cleanUser = username?.Trim() ?? string.Empty;
        var cleanKey = apiKey?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cleanUser) || string.IsNullOrWhiteSpace(cleanKey))
        {
            return (false, "Username and API key must not be empty.");
        }

        try
        {
            var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/favorites.json?limit=1");
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{cleanUser}:{cleanKey}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, $"Logged in successfully as {cleanUser}.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (false, "Invalid username or API key.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return (false, "Access forbidden. Ensure API access is enabled on your e621 account.");
            }

            return (false, $"Verification failed (HTTP {(int)response.StatusCode} {response.ReasonPhrase}).");
        }
        catch (OperationCanceledException)
        {
            return (false, "Verification request timed out.");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<ImagePost>> GetFavoritesAsync(int page = 1, int limit = 40, CancellationToken cancellationToken = default)
    {
        if (!AppSettings.IsLoggedIn || string.IsNullOrWhiteSpace(AppSettings.Username))
        {
            return Array.Empty<ImagePost>();
        }

        var effectiveLimit = Math.Clamp(limit, 1, 320);
        var effectivePage = Math.Clamp(page, 1, 750);
        var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');

        // Strategy 1: Attempt direct /favorites.json
        try
        {
            var url = $"{baseUrl}/favorites.json?limit={effectiveLimit}&page={effectivePage}";
            using var request = CreateRequest(HttpMethod.Get, url);
            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var result = await JsonSerializer.DeserializeAsync<E621PostsResponse>(stream, cancellationToken: cancellationToken);
                if (result?.Posts != null && result.Posts.Count > 0)
                {
                    return result.Posts.Select(MapToImagePost).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[E621ApiClient] /favorites.json query failed: {ex.Message}");
        }

        // Strategy 2: Standard e621 tag search /posts.json?tags=fav:{username}
        return await SearchPostsAsync($"fav:{AppSettings.Username}", effectivePage, effectiveLimit, cancellationToken);
    }

    public async Task<int?> GetFavoritesCountAsync(CancellationToken cancellationToken = default)
    {
        if (!AppSettings.IsLoggedIn)
        {
            return null;
        }

        var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');

        // Strategy 1: Check /users/me.json (direct authenticated user profile)
        try
        {
            var meUrl = $"{baseUrl}/users/me.json";
            using var meRequest = CreateRequest(HttpMethod.Get, meUrl);
            using var meResponse = await HttpClient.SendAsync(meRequest, cancellationToken);
            if (meResponse.IsSuccessStatusCode)
            {
                await using var stream = await meResponse.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("favorite_count", out var favCountProp) && favCountProp.TryGetInt32(out var favCount))
                    {
                        return favCount;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[E621ApiClient] /users/me.json count query failed: {ex.Message}");
        }

        // Strategy 2: Check /users.json?search[name_matches]={username}
        if (!string.IsNullOrWhiteSpace(AppSettings.Username))
        {
            try
            {
                var userUrl = $"{baseUrl}/users.json?search[name_matches]={Uri.EscapeDataString(AppSettings.Username.Trim())}";
                using var request = CreateRequest(HttpMethod.Get, userUrl);
                using var response = await HttpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    var root = doc.RootElement;
                    JsonElement? userElement = null;

                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        userElement = root[0];
                    }
                    else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("users", out var usersArr) && usersArr.ValueKind == JsonValueKind.Array && usersArr.GetArrayLength() > 0)
                    {
                        userElement = usersArr[0];
                    }

                    if (userElement.HasValue && userElement.Value.TryGetProperty("favorite_count", out var countProp) && countProp.TryGetInt32(out var count))
                    {
                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                StorageLocations.Log($"[E621ApiClient] /users.json count query failed: {ex.Message}");
            }
        }

        // Strategy 3: Check /favorites.json or /posts.json?tags=fav:{username}&limit=320
        try
        {
            var favs = await GetFavoritesAsync(page: 1, limit: 320, cancellationToken);
            if (favs != null)
            {
                return favs.Count;
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[E621ApiClient] GetFavoritesAsync count query failed: {ex.Message}");
        }

        return null;
    }

    public async Task<bool> AddFavoriteAsync(long postId, CancellationToken cancellationToken = default)
    {
        if (postId <= 0) return false;
        if (!AppSettings.IsLoggedIn) return false;

        try
        {
            var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/favorites.json";
            using var request = CreateRequest(HttpMethod.Post, url);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("post_id", postId.ToString())
            });

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            // 422 indicates already favorited on e621
            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveFavoriteAsync(long postId, CancellationToken cancellationToken = default)
    {
        if (postId <= 0) return false;
        if (!AppSettings.IsLoggedIn) return false;

        try
        {
            var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/favorites/{postId}.json";
            using var request = CreateRequest(HttpMethod.Delete, url);

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, int NewScore, int? OurScore)> VotePostAsync(long postId, int score, CancellationToken cancellationToken = default)
    {
        if (postId <= 0) return (false, 0, null);
        if (!AppSettings.IsLoggedIn) return (false, 0, null);

        try
        {
            var baseUrl = AppSettings.ApiBaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/posts/{postId}/votes.json";
            using var request = CreateRequest(HttpMethod.Post, url);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("score", score.ToString()),
                new KeyValuePair<string, string>("no_unvote", "false")
            });

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (false, 0, null);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var voteResult = await JsonSerializer.DeserializeAsync<E621VoteResponseDto>(stream, cancellationToken: cancellationToken);

            var total = voteResult?.Score?.Total ?? 0;
            var ourScore = voteResult?.OurScore;

            return (true, total, ourScore);
        }
        catch
        {
            return (false, 0, null);
        }
    }
}
