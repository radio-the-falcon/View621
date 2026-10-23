using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace View621.Models;

/// <summary>
/// Represents an image post metadata and asset references.
/// </summary>
public partial class ImagePost : ObservableObject
{
    private static readonly SolidColorBrush PinnedBackgroundBrush = new(Windows.UI.Color.FromArgb(255, 255, 184, 0));
    private static readonly SolidColorBrush PinnedBorderBrush = new(Windows.UI.Color.FromArgb(255, 245, 158, 11));
    private static readonly SolidColorBrush PinnedIconBrush = new(Windows.UI.Color.FromArgb(255, 20, 20, 20));

    private static readonly SolidColorBrush UnpinnedBackgroundBrush = new(Windows.UI.Color.FromArgb(200, 30, 30, 30));
    private static readonly SolidColorBrush UnpinnedBorderBrush = new(Windows.UI.Color.FromArgb(80, 255, 255, 255));
    private static readonly SolidColorBrush UnpinnedIconBrush = new(Windows.UI.Color.FromArgb(255, 255, 255, 255));

    private bool _isBookmarked;
    public bool IsBookmarked
    {
        get => _isBookmarked;
        set
        {
            if (SetProperty(ref _isBookmarked, value))
            {
                OnPropertyChanged(nameof(BookmarkGlyph));
                OnPropertyChanged(nameof(BookmarkTooltip));
                OnPropertyChanged(nameof(BookmarkOpacity));
                OnPropertyChanged(nameof(BookmarkButtonBackground));
                OnPropertyChanged(nameof(BookmarkButtonBorder));
                OnPropertyChanged(nameof(BookmarkIconForeground));
            }
        }
    }

    public string BookmarkGlyph => IsBookmarked ? "\uE840" : "\uE718";
    public string BookmarkTooltip => IsBookmarked ? "Unpin post" : "Pin post";
    public double BookmarkOpacity => IsBookmarked ? 1.0 : 0.75;

    public Brush BookmarkButtonBackground => IsBookmarked ? PinnedBackgroundBrush : UnpinnedBackgroundBrush;
    public Brush BookmarkButtonBorder => IsBookmarked ? PinnedBorderBrush : UnpinnedBorderBrush;
    public Brush BookmarkIconForeground => IsBookmarked ? PinnedIconBrush : UnpinnedIconBrush;

    private bool _isFavorited;
    public bool IsFavorited
    {
        get => _isFavorited;
        set => SetProperty(ref _isFavorited, value);
    }

    private int? _ourScore;
    public int? OurScore
    {
        get => _ourScore;
        set => SetProperty(ref _ourScore, value);
    }

    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = "Unknown";
    public string Rating { get; set; } = "Safe"; // Safe, Questionable, Explicit
    public int Score { get; set; }
    public int FavCount { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string FileExtension { get; set; } = "jpg";
    public string FileSizeFormatted { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsVideo
    {
        get
        {
            var ext = FileExtension?.Trim().ToLowerInvariant() ?? string.Empty;
            return ext is "webm" or "mp4" or "mkv" or "avi" or "mov" or "flv";
        }
    }

    public bool IsAnimatedGif
    {
        get
        {
            var ext = FileExtension?.Trim().ToLowerInvariant() ?? string.Empty;
            return ext is "gif";
        }
    }

    /// <summary>
    /// Type label for UI badge: 'Video', 'GIF', or 'Image'.
    /// </summary>
    public string MediaTypeLabel => IsVideo ? "Video" : (IsAnimatedGif ? "GIF" : "Image");

    /// <summary>
    /// Glyph icon for UI badge: Video (\uE714), GIF/Animation (\uE790), Image (\uEB9F).
    /// </summary>
    public string MediaTypeGlyph => IsVideo ? "\uE714" : (IsAnimatedGif ? "\uE790" : "\uEB9F");

    /// <summary>
    /// Small thumbnail URL for gallery and search grid previews (~150px).
    /// </summary>
    public string PreviewUrl { get; set; } = string.Empty;

    /// <summary>
    /// Reasonable size preview/sample image URL (usually 850px-1280px) for detail viewing without loading giant original files.
    /// </summary>
    public string SampleUrl { get; set; } = string.Empty;

    /// <summary>
    /// Full original resolution file URL.
    /// </summary>
    public string FullUrl { get; set; } = string.Empty;

    public List<string> GeneralTags { get; set; } = new();
    public List<string> ArtistTags { get; set; } = new();
    public List<string> CharacterTags { get; set; } = new();
    public List<string> SpeciesTags { get; set; } = new();
    public List<string> CopyrightTags { get; set; } = new();

    public List<string> Sources { get; set; } = new();
    public string Description { get; set; } = string.Empty;

    public long? ParentId { get; set; }
    public bool HasChildren { get; set; }
    public List<long> ChildIds { get; set; } = new();
    public List<long> PoolIds { get; set; } = new();

    public bool HasParent => ParentId.HasValue && ParentId.Value > 0;
    public bool HasRelationships => HasParent || HasChildren;
    public bool IsInPool => PoolIds.Count > 0;

    public string DimensionText => $"{Width} × {Height}";
    public string DisplayName => string.IsNullOrWhiteSpace(Title) ? $"Post #{Id} ({Artist})" : Title;
    public string ScoreText => $"Score: {Score}";
    public string FavouritesText => $"Favourites: {FavCount}";

    /// <summary>
    /// Local cached sample image path on disk if downloaded to ~/.View621/cache/images.
    /// </summary>
    public string? LocalSamplePath { get; set; }

    /// <summary>
    /// Safely parsed Uri for PreviewUrl, preventing XAML parser crashes on invalid or empty strings.
    /// </summary>
    public Uri? PreviewUri => (!string.IsNullOrWhiteSpace(PreviewUrl) && Uri.TryCreate(PreviewUrl, UriKind.Absolute, out var uri)) ? uri : null;

    /// <summary>
    /// Safely parsed Uri for SampleUrl.
    /// </summary>
    public Uri? SampleUri => (!string.IsNullOrWhiteSpace(SampleUrl) && Uri.TryCreate(SampleUrl, UriKind.Absolute, out var uri)) ? uri : null;

    /// <summary>
    /// Safely parsed Uri for FullUrl.
    /// </summary>
    public Uri? FullUri => (!string.IsNullOrWhiteSpace(FullUrl) && Uri.TryCreate(FullUrl, UriKind.Absolute, out var uri)) ? uri : null;

    /// <summary>
    /// Effective URI for sample / detail viewing. Prefers the local cached sample image if present on disk.
    /// </summary>
    public Uri? EffectiveSampleUri
    {
        get
        {
            var localPath = LocalSamplePath;
            if (string.IsNullOrWhiteSpace(localPath) || !System.IO.File.Exists(localPath))
            {
                localPath = View621.Services.StorageLocations.GetExistingCachedImagePath(Id);
                if (localPath != null)
                {
                    LocalSamplePath = localPath;
                }
            }

            if (!string.IsNullOrWhiteSpace(localPath) && System.IO.File.Exists(localPath))
            {
                try
                {
                    return new Uri(localPath);
                }
                catch { }
            }

            return SampleUri ?? PreviewUri ?? FullUri;
        }
    }

    /// <summary>
    /// Effective URI for gallery cards. Uses local cached file if available offline, otherwise PreviewUri.
    /// </summary>
    public Uri? EffectivePreviewUri
    {
        get
        {
            var localPath = LocalSamplePath;
            if (string.IsNullOrWhiteSpace(localPath) || !System.IO.File.Exists(localPath))
            {
                localPath = View621.Services.StorageLocations.GetExistingCachedImagePath(Id);
                if (localPath != null)
                {
                    LocalSamplePath = localPath;
                }
            }

            if (!string.IsNullOrWhiteSpace(localPath) && System.IO.File.Exists(localPath))
            {
                try
                {
                    return new Uri(localPath);
                }
                catch { }
            }

            return PreviewUri;
        }
    }

    public bool HasValidPreview => PreviewUri != null || EffectivePreviewUri != null;
}
