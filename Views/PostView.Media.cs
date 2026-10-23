using System;
using System.IO;
using Microsoft.UI.Xaml;
using Windows.Media.Core;
using Windows.Media.Playback;
using View621.Models;
using View621.Services;

namespace View621.Views;

public sealed partial class PostView
{
    private void PlayVideoPost(ImagePost post)
    {
        try
        {
            var targetUrl = !string.IsNullOrWhiteSpace(post.FullUrl)
                ? post.FullUrl
                : (!string.IsNullOrWhiteSpace(post.SampleUrl) ? post.SampleUrl : post.PreviewUrl);

            var localCached = StorageLocations.GetExistingCachedImagePath(post.Id);
            Uri? videoUri = null;

            if (!string.IsNullOrWhiteSpace(localCached) && File.Exists(localCached))
            {
                videoUri = new Uri(localCached);
            }
            else if (!string.IsNullOrWhiteSpace(targetUrl) && Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri))
            {
                videoUri = uri;
            }

            if (videoUri != null)
            {
                var mediaPlayer = DetailMediaPlayer.MediaPlayer;
                if (mediaPlayer == null)
                {
                    mediaPlayer = new MediaPlayer();
                    DetailMediaPlayer.SetMediaPlayer(mediaPlayer);
                }

                mediaPlayer.IsLoopingEnabled = true;
                mediaPlayer.AutoPlay = true;

                var source = MediaSource.CreateFromUri(videoUri);
                source.OpenOperationCompleted += (s, e) =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        ImageLoadingRing.IsActive = false;
                        ImageLoadingRing.Visibility = Visibility.Collapsed;
                    });
                };

                mediaPlayer.Source = source;

                if (string.IsNullOrWhiteSpace(localCached) || !File.Exists(localCached))
                {
                    _ = ImageCacheService.Instance.CacheSampleImageAsync(post);
                }
            }
            else
            {
                ImageLoadingRing.IsActive = false;
                ImageLoadingRing.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[Video Playback Error] Post #{post.Id}: {ex.Message}");
            ImageLoadingRing.IsActive = false;
            ImageLoadingRing.Visibility = Visibility.Collapsed;
        }

        ApplyFitState();
    }

    private void PauseVideoPlayer()
    {
        try
        {
            var player = DetailMediaPlayer?.MediaPlayer;
            if (player != null && player.PlaybackSession.CanPause)
            {
                player.Pause();
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[MediaPlayer Pause Error] {ex.Message}");
        }
    }

    private void StopAndCleanupMediaPlayer()
    {
        try
        {
            var player = DetailMediaPlayer?.MediaPlayer;
            if (player != null)
            {
                player.Pause();
                player.Source = null;
            }
        }
        catch (Exception ex)
        {
            StorageLocations.Log($"[MediaPlayer Cleanup Error] {ex.Message}");
        }
    }
}
