using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.System;
using E6Viewer.Models;
using E6Viewer.Services;
using E6Viewer.ViewModels;
using PostNavMode = E6Viewer.Models.NavigationMode;

namespace E6Viewer.Views;

/// <summary>
/// Canonical view for inspecting individual posts, media playback, zoom/pan manipulation, and relations.
/// </summary>
public sealed partial class PostView : Page
{
    public ImagePost Post { get; private set; } = new();
    public PostViewModel ViewModel { get; } = new();

    private PostNavigationContext _navContext = new();
    private bool _isOriginalView;
    private bool _isFitToFill;

    private readonly IImageService _imageService = new E621ApiClient();
    private readonly DispatcherTimer _poolIndicatorFadeTimer = new();

    public PostView()
    {
        InitializeComponent();
        SizeChanged += PostView_SizeChanged;
        ViewportContainer.SizeChanged += ViewportContainer_SizeChanged;
        ImageScrollViewer.SizeChanged += ImageScrollViewer_SizeChanged;
        DetailMediaPlayer.SizeChanged += DetailMediaPlayer_SizeChanged;
        KeyDown += PostView_KeyDown;

        _poolIndicatorFadeTimer.Interval = TimeSpan.FromSeconds(2);
        _poolIndicatorFadeTimer.Tick += PoolIndicatorFadeTimer_Tick;

        BookmarkService.Instance.BookmarksChanged += BookmarkService_BookmarksChanged;
        NavigationCacheMode = NavigationCacheMode.Enabled;
        Loaded += PostView_Loaded;
        Unloaded += PostView_Unloaded;
    }

    private void PostView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Post != null && Post.IsVideo)
        {
            DetailMediaPlayer.Visibility = Visibility.Visible;
            ImageScrollViewer.Visibility = Visibility.Collapsed;
            ApplyFitState();

            DispatcherQueue.TryEnqueue(() =>
            {
                if (DetailMediaPlayer != null)
                {
                    DetailMediaPlayer.AreTransportControlsEnabled = false;
                    DetailMediaPlayer.AreTransportControlsEnabled = true;
                }
            });
        }
    }

    private void PostView_Unloaded(object sender, RoutedEventArgs e)
    {
        PauseVideoPlayer();
    }

    private void BookmarkService_BookmarksChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateBookmarkUI);
    }

    private void PostView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isOriginalView || _isFitToFill)
        {
            ApplyFitState();
        }
    }

    private void ViewportContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isOriginalView || _isFitToFill)
        {
            ApplyFitState();
        }
    }

    private void ImageScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isOriginalView || _isFitToFill)
        {
            ApplyFitState();
        }
    }

    private void DetailMediaPlayer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (Post.IsVideo && (!_isOriginalView || _isFitToFill))
        {
            ApplyFitState();
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is PostNavigationContext context)
        {
            _navContext = context;
            if (_navContext.Mode == PostNavMode.Pool && (_navContext.Posts.Count == 0 || _navContext.PoolPostIds == null))
            {
                await LoadPoolAsync(_navContext.PoolId ?? 0);
            }
            else if (_navContext.Posts.Count > 0 && _navContext.CurrentIndex >= 0 && _navContext.CurrentIndex < _navContext.Posts.Count)
            {
                LoadPost(_navContext.Posts[_navContext.CurrentIndex]);
            }
        }
        else if (e.Parameter is ImagePost post)
        {
            _navContext = new PostNavigationContext
            {
                Mode = PostNavMode.SearchResults,
                Posts = new List<ImagePost> { post },
                CurrentIndex = 0
            };
            LoadPost(post);
        }
    }

    public void LoadPost(ImagePost post)
    {
        Post = post;
        ViewModel.SetPost(post);
        _isOriginalView = false;
        ResolutionButtonText.Text = "Switch to full view";

        _isFitToFill = !IsExemptFromFit();
        Bindings.Update();

        ImageLoadingRing.IsActive = true;
        ImageLoadingRing.Visibility = Visibility.Visible;

        if (post.IsVideo)
        {
            ImageScrollViewer.Visibility = Visibility.Collapsed;
            DetailMediaPlayer.Visibility = Visibility.Visible;
            PlayVideoPost(post);
        }
        else
        {
            StopAndCleanupMediaPlayer();
            DetailMediaPlayer.Visibility = Visibility.Collapsed;
            ImageScrollViewer.Visibility = Visibility.Visible;

            if (post.EffectiveSampleUri != null)
            {
                DetailImage.Source = new BitmapImage(post.EffectiveSampleUri);
            }
            else if (post.EffectivePreviewUri != null)
            {
                DetailImage.Source = new BitmapImage(post.EffectivePreviewUri);
            }

            var localCached = StorageLocations.GetExistingCachedImagePath(post.Id);
            if (string.IsNullOrWhiteSpace(localCached) || !File.Exists(localCached))
            {
                var currentPostId = post.Id;
                _ = Task.Run(async () =>
                {
                    var cachedPath = await ImageCacheService.Instance.CacheSampleImageAsync(post);
                    if (!string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath))
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            if (Post != null && Post.Id == currentPostId && !_isOriginalView && !Post.IsVideo)
                            {
                                if (post.EffectiveSampleUri != null)
                                {
                                    DetailImage.Source = new BitmapImage(post.EffectiveSampleUri);
                                }
                            }
                        });
                    }
                });
            }
        }

        UpdateFitButtonAvailability();
        UpdateNavigationButtons();
        UpdateRelationshipsAndPoolUI();
        PostCacheService.Instance.CachePost(post);
        UpdateBookmarkUI();
    }

    private void UpdateBookmarkUI()
    {
        if (Post == null || Post.Id <= 0) return;
        bool isBookmarked = BookmarkService.Instance.IsBookmarked(Post.Id);
        Post.IsBookmarked = isBookmarked;
        BookmarkIcon.Glyph = isBookmarked ? "\uE840" : "\uE718";
        ToolTipService.SetToolTip(BookmarkButton, isBookmarked ? "Unpin post" : "Pin post");

        if (isBookmarked)
        {
            BookmarkButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 184, 0));
            BookmarkButton.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11));
            BookmarkIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));
        }
        else
        {
            BookmarkButton.ClearValue(Button.BackgroundProperty);
            BookmarkButton.ClearValue(Button.BorderBrushProperty);
            BookmarkIcon.ClearValue(FontIcon.ForegroundProperty);
        }
    }

    private void BookmarkButton_Click(object sender, RoutedEventArgs e)
    {
        if (Post != null && Post.Id > 0)
        {
            BookmarkService.Instance.ToggleBookmark(Post.Id, Post);
            UpdateBookmarkUI();
        }
    }

    private void DetailImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        ImageLoadingRing.IsActive = false;
        ImageLoadingRing.Visibility = Visibility.Collapsed;
    }

    private void DetailImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        ImageLoadingRing.IsActive = false;
        ImageLoadingRing.Visibility = Visibility.Collapsed;
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        StopAndCleanupMediaPlayer();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        StopAndCleanupMediaPlayer();
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            TabService.Instance.CloseCurrentTab();
        }
    }

    private void CopyLink_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(Post.FullUrl);
        Clipboard.SetContent(package);
    }

    private void CopyImage_Click(object sender, RoutedEventArgs e)
    {
        if (Post.FullUri != null)
        {
            var package = new DataPackage();
            package.SetBitmap(RandomAccessStreamReference.CreateFromUri(Post.FullUri));
            Clipboard.SetContent(package);
        }
        else if (Post.SampleUri != null)
        {
            var package = new DataPackage();
            package.SetBitmap(RandomAccessStreamReference.CreateFromUri(Post.SampleUri));
            Clipboard.SetContent(package);
        }
    }

    private async void OpenInBrowser_Click(object sender, RoutedEventArgs e)
    {
        var url = $"{AppSettings.ApiBaseUrl.TrimEnd('/')}/posts/{Post.Id}";
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            await Launcher.LaunchUriAsync(uri);
        }
    }

    private void ToggleResolution_Click(object sender, RoutedEventArgs e)
    {
        _isOriginalView = !_isOriginalView;
        try
        {
            if (_isOriginalView)
            {
                if (Post.FullUri != null)
                {
                    DetailImage.Source = new BitmapImage(Post.FullUri);
                }
                else if (Uri.TryCreate(Post.FullUrl, UriKind.Absolute, out var fullUri))
                {
                    DetailImage.Source = new BitmapImage(fullUri);
                }
                ResolutionButtonText.Text = "Switch to sample view";
            }
            else
            {
                if (Post.EffectiveSampleUri != null)
                {
                    DetailImage.Source = new BitmapImage(Post.EffectiveSampleUri);
                }
                else if (Uri.TryCreate(Post.SampleUrl, UriKind.Absolute, out var sampleUri))
                {
                    DetailImage.Source = new BitmapImage(sampleUri);
                }
                ResolutionButtonText.Text = "Switch to full view";
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText("startup_error.log", $"[Resolution Switch Error] {ex}\n");
        }

        UpdateFitButtonAvailability();
    }

    private async void SaveImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            if (App.CurrentWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            }

            var ext = Path.GetExtension(Post.FullUrl);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
            if (!ext.StartsWith('.')) ext = "." + ext;

            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            savePicker.FileTypeChoices.Add($"Image / Media ({ext})", new List<string> { ext });
            savePicker.SuggestedFileName = $"e621_{Post.Id}{ext}";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Umamiff/1.0 (by Antigravity on e621)");
                var bytes = await httpClient.GetByteArrayAsync(Post.FullUrl);
                await Windows.Storage.FileIO.WriteBytesAsync(file, bytes);
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText("startup_error.log", $"[SaveImage Error] {ex}\n");
        }
    }
}
