using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using View621.Models;
using View621.Services;
using PostNavMode = View621.Models.NavigationMode;

namespace View621.Views;

public sealed partial class PostView
{
    private void PoolIndicatorFadeTimer_Tick(object? sender, object e)
    {
        _poolIndicatorFadeTimer.Stop();
        PoolToolbar.Visibility = Visibility.Collapsed;
    }

    private void PostView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Left)
        {
            if (PreviousPostButton.IsEnabled)
            {
                e.Handled = true;
                NavigateToPostAt(_navContext.CurrentIndex - 1);
            }
        }
        else if (e.Key == VirtualKey.Right)
        {
            if (NextPostButton.IsEnabled)
            {
                e.Handled = true;
                NavigateToPostAt(_navContext.CurrentIndex + 1);
            }
        }
    }

    private void UpdateNavigationButtons()
    {
        if (_navContext.Mode == PostNavMode.SearchResults)
        {
            PreviousPostButton.IsEnabled = _navContext.CurrentIndex > 0;
            NextPostButton.IsEnabled = _navContext.CurrentIndex < _navContext.Posts.Count - 1;
            PoolToolbar.Visibility = Visibility.Collapsed;
            _poolIndicatorFadeTimer.Stop();
        }
        else if (_navContext.Mode == PostNavMode.Pool)
        {
            PreviousPostButton.IsEnabled = _navContext.CurrentIndex > 0;
            NextPostButton.IsEnabled = _navContext.PoolPostIds != null && _navContext.CurrentIndex < _navContext.PoolPostIds.Count - 1;

            PoolToolbar.Visibility = Visibility.Visible;
            var poolTitle = _navContext.PoolName ?? $"Pool #{_navContext.PoolId}";
            var totalCount = _navContext.PoolPostIds?.Count ?? 0;
            PoolToolbarText.Text = $"{poolTitle} ({_navContext.CurrentIndex + 1}/{totalCount})";

            _poolIndicatorFadeTimer.Stop();
            _poolIndicatorFadeTimer.Start();
        }
    }

    private async void UpdateRelationshipsAndPoolUI()
    {
        bool hasRelationships = false;

        if (Post.HasParent && Post.ParentId.HasValue)
        {
            ParentSection.Visibility = Visibility.Visible;
            ParentButtonText.Text = $"Post #{Post.ParentId.Value}";
            hasRelationships = true;
        }
        else
        {
            ParentSection.Visibility = Visibility.Collapsed;
        }

        if (Post.IsInPool)
        {
            hasRelationships = true;
            PoolsSection.Visibility = Visibility.Visible;
            SidebarViewPoolButton.Visibility = Visibility.Visible;

            _ = Task.Run(async () =>
            {
                var poolDtos = new List<E621PoolDto>();
                foreach (var poolId in Post.PoolIds)
                {
                    var pool = await _imageService.GetPoolByIdAsync(poolId);
                    if (pool != null) poolDtos.Add(pool);
                }

                DispatcherQueue.TryEnqueue(() =>
                {
                    PoolsItemsControl.ItemsSource = poolDtos;
                });
            });
        }
        else if (_navContext.Mode == View621.Models.NavigationMode.Pool && _navContext.PoolId.HasValue)
        {
            hasRelationships = true;
            PoolsSection.Visibility = Visibility.Visible;
            SidebarViewPoolButton.Visibility = Visibility.Visible;
        }
        else
        {
            PoolsSection.Visibility = Visibility.Collapsed;
            SidebarViewPoolButton.Visibility = Visibility.Collapsed;
        }

        if (Post.HasChildren && Post.ChildIds.Count > 0)
        {
            hasRelationships = true;
            ChildrenSection.Visibility = Visibility.Visible;
            ChildrenItemsControl.ItemsSource = Post.ChildIds;
        }
        else
        {
            ChildrenSection.Visibility = Visibility.Collapsed;
        }

        RelationshipsCard.Visibility = hasRelationships ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task LoadPoolAsync(long poolId)
    {
        var pool = await _imageService.GetPoolByIdAsync(poolId);
        if (pool != null && pool.PostIds.Count > 0)
        {
            _navContext.PoolId = pool.Id;
            _navContext.PoolName = pool.Name.Replace("_", " ");
            _navContext.PoolPostIds = pool.PostIds;
            _navContext.CurrentIndex = 0;

            await LoadPoolPostAtIndexAsync(0);
        }
    }

    private async Task LoadPoolPostAtIndexAsync(int index)
    {
        if (_navContext.PoolPostIds == null || index < 0 || index >= _navContext.PoolPostIds.Count) return;

        var postId = _navContext.PoolPostIds[index];
        _navContext.CurrentIndex = index;
        UpdateNavigationButtons();

        ImageLoadingRing.IsActive = true;
        ImageLoadingRing.Visibility = Visibility.Visible;

        var post = await _imageService.GetPostByIdAsync(postId);
        if (post != null)
        {
            LoadPost(post);
        }
        else
        {
            ImageLoadingRing.IsActive = false;
            ImageLoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    private async void NavigateToPostAt(int newIndex)
    {
        if (_navContext.Mode == PostNavMode.SearchResults)
        {
            if (newIndex >= 0 && newIndex < _navContext.Posts.Count)
            {
                _navContext.CurrentIndex = newIndex;
                UpdateNavigationButtons();

                ImageLoadingRing.IsActive = true;
                ImageLoadingRing.Visibility = Visibility.Visible;

                LoadPost(_navContext.Posts[newIndex]);
            }
        }
        else if (_navContext.Mode == PostNavMode.Pool)
        {
            await LoadPoolPostAtIndexAsync(newIndex);
        }
    }

    private void PreviousPost_Click(object sender, RoutedEventArgs e)
    {
        NavigateToPostAt(_navContext.CurrentIndex - 1);
    }

    private void NextPost_Click(object sender, RoutedEventArgs e)
    {
        NavigateToPostAt(_navContext.CurrentIndex + 1);
    }

    private void TagChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            TabService.Instance.OpenSearchInNewTab(tag);
        }
    }

    private void TagChipOpen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string tag })
        {
            Frame.Navigate(typeof(SearchView), tag);
        }
    }

    private void TagChipOpenInNewTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string tag })
        {
            TabService.Instance.OpenSearchInNewTab(tag);
        }
    }

    private async void ParentButton_Click(object sender, RoutedEventArgs e)
    {
        if (Post.ParentId.HasValue)
        {
            var parentPost = await _imageService.GetPostByIdAsync(Post.ParentId.Value);
            if (parentPost != null)
            {
                _navContext.Posts = new List<ImagePost> { parentPost };
                _navContext.CurrentIndex = 0;
                LoadPost(parentPost);
            }
        }
    }

    private async void ParentButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(sender as UIElement).Properties;
        if (properties.IsMiddleButtonPressed && Post.ParentId.HasValue)
        {
            e.Handled = true;
            var parentPost = await _imageService.GetPostByIdAsync(Post.ParentId.Value);
            if (parentPost != null)
            {
                TabService.Instance.OpenPostInNewTab(parentPost);
            }
        }
    }

    private async void PoolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: long poolId })
        {
            _navContext.Mode = PostNavMode.Pool;
            await LoadPoolAsync(poolId);
        }
    }

    private async void ChildButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: long childId })
        {
            var childPost = await _imageService.GetPostByIdAsync(childId);
            if (childPost != null)
            {
                _navContext.Posts = new List<ImagePost> { childPost };
                _navContext.CurrentIndex = 0;
                LoadPost(childPost);
            }
        }
    }

    private async void ChildButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(sender as UIElement).Properties;
        if (properties.IsMiddleButtonPressed && sender is FrameworkElement fe && fe.DataContext is long childId)
        {
            e.Handled = true;
            var childPost = await _imageService.GetPostByIdAsync(childId);
            if (childPost != null)
            {
                TabService.Instance.OpenPostInNewTab(childPost);
            }
        }
    }

    private void OpenPoolFull_Click(object sender, RoutedEventArgs e)
    {
        if (_navContext.PoolId.HasValue)
        {
            TabService.Instance.OpenSearchInNewTab($"pool:{_navContext.PoolId.Value}");
        }
    }
}
