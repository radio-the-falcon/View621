using System;
using CommunityToolkit.Mvvm.ComponentModel;
using View621.Models;
using View621.Services;

namespace View621.ViewModels;

/// <summary>
/// ViewModel managing presentation state, resolution toggling, and bookmark/pin state for PostView.
/// </summary>
public partial class PostViewModel : ObservableObject
{
    private readonly IBookmarkService _bookmarkService;
    private readonly IImageService _imageService;

    public PostViewModel() : this(BookmarkService.Instance, new E621ApiClient())
    {
    }

    public PostViewModel(IBookmarkService bookmarkService, IImageService imageService)
    {
        _bookmarkService = bookmarkService;
        _imageService = imageService;
    }

    [ObservableProperty]
    public partial ImagePost Post { get; set; } = new();

    [ObservableProperty]
    public partial PostNavigationContext NavContext { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResolutionButtonText))]
    public partial bool IsOriginalView { get; set; }

    [ObservableProperty]
    public partial bool IsFitToFill { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BookmarkGlyph))]
    [NotifyPropertyChangedFor(nameof(BookmarkTooltip))]
    public partial bool IsBookmarked { get; set; }

    [ObservableProperty]
    public partial bool IsFavorited { get; set; }

    [ObservableProperty]
    public partial int? OurScore { get; set; }

    [ObservableProperty]
    public partial int Score { get; set; }

    [ObservableProperty]
    public partial int FavCount { get; set; }

    public string ResolutionButtonText => IsOriginalView ? "Switch to sample view" : "Switch to full view";
    public string BookmarkGlyph => IsBookmarked ? "\uE840" : "\uE718";
    public string BookmarkTooltip => IsBookmarked ? "Unpin post" : "Pin post";

    public void SetPost(ImagePost post)
    {
        Post = post;
        IsOriginalView = false;
        IsBookmarked = _bookmarkService.IsBookmarked(post.Id);
        post.IsBookmarked = IsBookmarked;

        IsFavorited = post.IsFavorited;
        OurScore = post.OurScore;
        Score = post.Score;
        FavCount = post.FavCount;
    }

    public void ToggleBookmark()
    {
        if (Post.Id <= 0) return;
        IsBookmarked = _bookmarkService.ToggleBookmark(Post.Id, Post);
    }

    public void SyncBookmarkState()
    {
        if (Post.Id > 0)
        {
            IsBookmarked = _bookmarkService.IsBookmarked(Post.Id);
            Post.IsBookmarked = IsBookmarked;
        }
    }
}
