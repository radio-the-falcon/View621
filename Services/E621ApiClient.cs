using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using E6Viewer.Models;

namespace E6Viewer.Services;

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
        client.DefaultRequestHeaders.UserAgent.ParseAdd("View621/1.0 (Desktop client; https://github.com/e6_viewer)");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);



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
}
