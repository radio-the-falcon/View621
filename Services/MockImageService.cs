using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using E6Viewer.Models;

namespace E6Viewer.Services;

/// <summary>
/// Provides mock data for development and initial UI testing.
/// </summary>
public class MockImageService : IImageService
{
    private static readonly List<ImagePost> SamplePosts = new()
    {
        new ImagePost
        {
            Id = 1001,
            Title = "Sunset Over High Mountain Pass",
            Artist = "aurora_artist",
            Rating = "General",
            Score = 142,
            Width = 1920,
            Height = 1080,
            FileExtension = "jpg",
            FileSizeFormatted = "2.4 MB",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            PreviewUrl = "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=400",
            SampleUrl = "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=1280",
            FullUrl = "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=1920",
            ArtistTags = new() { "aurora_artist" },
            CharacterTags = new() { "scenery" },
            SpeciesTags = new() { "landscape" },
            GeneralTags = new() { "mountain", "sunset", "clouds", "vibrant", "sky" }
        },
        new ImagePost
        {
            Id = 1002,
            Title = "Misty Evergreen Woodland Trail",
            Artist = "sylvan_fox",
            Rating = "General",
            Score = 98,
            Width = 1600,
            Height = 1200,
            FileExtension = "png",
            FileSizeFormatted = "3.1 MB",
            CreatedAt = DateTime.UtcNow.AddDays(-4),
            PreviewUrl = "https://images.unsplash.com/photo-1448375240586-882707db888b?w=400",
            SampleUrl = "https://images.unsplash.com/photo-1448375240586-882707db888b?w=1280",
            FullUrl = "https://images.unsplash.com/photo-1448375240586-882707db888b?w=1600",
            ArtistTags = new() { "sylvan_fox" },
            CharacterTags = new() { "nature" },
            SpeciesTags = new() { "forest" },
            GeneralTags = new() { "trees", "fog", "morning", "moss", "atmospheric" }
        },
        new ImagePost
        {
            Id = 1003,
            Title = "Arctic Fox in Deep Snow",
            Artist = "frostpaw",
            Rating = "General",
            Score = 320,
            Width = 2048,
            Height = 1365,
            FileExtension = "jpg",
            FileSizeFormatted = "1.8 MB",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            PreviewUrl = "https://images.unsplash.com/photo-1517849845537-4d257902454a?w=400",
            SampleUrl = "https://images.unsplash.com/photo-1517849845537-4d257902454a?w=1280",
            FullUrl = "https://images.unsplash.com/photo-1517849845537-4d257902454a?w=2048",
            ArtistTags = new() { "frostpaw" },
            CharacterTags = new() { "canine" },
            SpeciesTags = new() { "fox", "arctic_fox" },
            GeneralTags = new() { "snow", "winter", "portrait", "white_fur", "fluffy" }
        },
        new ImagePost
        {
            Id = 1004,
            Title = "Neon Cyberpunk Alleyway",
            Artist = "glitch_runner",
            Rating = "General",
            Score = 215,
            Width = 1920,
            Height = 1080,
            FileExtension = "png",
            FileSizeFormatted = "4.2 MB",
            CreatedAt = DateTime.UtcNow.AddDays(-6),
            PreviewUrl = "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=400",
            SampleUrl = "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=1280",
            FullUrl = "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=1920",
            ArtistTags = new() { "glitch_runner" },
            CharacterTags = new() { "urban" },
            SpeciesTags = new() { "scifi" },
            GeneralTags = new() { "cyberpunk", "neon", "rain", "reflections", "night" }
        },
        new ImagePost
        {
            Id = 1005,
            Title = "Cosmic Nebula with Starlight",
            Artist = "stellar_void",
            Rating = "General",
            Score = 180,
            Width = 1920,
            Height = 1200,
            FileExtension = "jpg",
            FileSizeFormatted = "3.8 MB",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            PreviewUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=400",
            SampleUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=1280",
            FullUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=1920",
            ArtistTags = new() { "stellar_void" },
            CharacterTags = new() { "space" },
            SpeciesTags = new() { "astronomy" },
            GeneralTags = new() { "stars", "galaxy", "nebula", "deep_space", "glow" }
        },
        new ImagePost
        {
            Id = 1006,
            Title = "Emerald Lake and Pine Reflection",
            Artist = "aurora_artist",
            Rating = "General",
            Score = 276,
            Width = 1920,
            Height = 1080,
            FileExtension = "jpg",
            FileSizeFormatted = "2.9 MB",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            PreviewUrl = "https://images.unsplash.com/photo-1470071459604-3b5ec3a7fe05?w=400",
            SampleUrl = "https://images.unsplash.com/photo-1470071459604-3b5ec3a7fe05?w=1280",
            FullUrl = "https://images.unsplash.com/photo-1470071459604-3b5ec3a7fe05?w=1920",
            ArtistTags = new() { "aurora_artist" },
            CharacterTags = new() { "scenery" },
            SpeciesTags = new() { "landscape" },
            GeneralTags = new() { "lake", "water", "pine_trees", "reflection", "nature" }
        }
    };

    public Task<IReadOnlyList<ImagePost>> SearchPostsAsync(string query, int page = 1, int limit = 30, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<ImagePost>>(SamplePosts);
        }

        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var filtered = SamplePosts.Where(post =>
        {
            var allTags = post.GeneralTags
                .Concat(post.ArtistTags)
                .Concat(post.CharacterTags)
                .Concat(post.SpeciesTags)
                .Concat(new[] { post.Title, post.Artist, post.Rating })
                .Select(t => t.ToLowerInvariant());

            return terms.All(term => allTags.Any(tag => tag.Contains(term)));
        }).ToList();

        return Task.FromResult<IReadOnlyList<ImagePost>>(filtered);
    }

    public Task<ImagePost?> GetPostByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var post = SamplePosts.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(post);
    }

    public Task<IReadOnlyList<string>> AutocompleteTagsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var lower = query.ToLowerInvariant();
        var matches = SamplePosts
            .SelectMany(p => p.GeneralTags.Concat(p.ArtistTags).Concat(p.SpeciesTags).Concat(p.CharacterTags))
            .Distinct()
            .Where(t => t.Contains(lower))
            .Take(10)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(matches);
    }

    public Task<E621PoolDto?> GetPoolByIdAsync(long poolId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<E621PoolDto?>(null);
    }

    public Task<int?> GetPostCountAsync(string query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<int?>(SamplePosts.Count);
    }
}
