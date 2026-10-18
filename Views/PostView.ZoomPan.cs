using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace E6Viewer.Views;

public sealed partial class PostView
{
    private bool _isDragging;
    private Point _lastDragPosition;

    private bool IsExemptFromFit()
    {
        return false;
    }

    private void UpdateFitButtonAvailability()
    {
        FitToViewButton.Visibility = _isOriginalView ? Visibility.Visible : Visibility.Collapsed;
        FitToViewButton.IsEnabled = _isOriginalView;
        ApplyFitState();
    }

    private void ApplyFitState()
    {
        bool shouldFit = !_isOriginalView || _isFitToFill;

        var viewportWidth = ViewportContainer.ActualWidth > 0 
            ? ViewportContainer.ActualWidth 
            : (ImageScrollViewer.ActualWidth > 0 ? ImageScrollViewer.ActualWidth : DetailMediaPlayer.ActualWidth);

        var viewportHeight = ViewportContainer.ActualHeight > 0 
            ? ViewportContainer.ActualHeight 
            : (ImageScrollViewer.ActualHeight > 0 ? ImageScrollViewer.ActualHeight : DetailMediaPlayer.ActualHeight);

        if (Post.IsVideo)
        {
            DetailMediaPlayer.ClearValue(FrameworkElement.WidthProperty);
            DetailMediaPlayer.ClearValue(FrameworkElement.HeightProperty);
            DetailMediaPlayer.ClearValue(FrameworkElement.MaxWidthProperty);
            DetailMediaPlayer.ClearValue(FrameworkElement.MaxHeightProperty);
            DetailMediaPlayer.Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform;
            return;
        }

        if (shouldFit)
        {
            if (viewportWidth > 0 && viewportHeight > 0 && Post.Width > 0 && Post.Height > 0)
            {
                var widthRatio = viewportWidth / Post.Width;
                var heightRatio = viewportHeight / Post.Height;
                var scale = Math.Min(widthRatio, heightRatio);

                DetailImage.Width = Math.Floor(Post.Width * scale);
                DetailImage.Height = Math.Floor(Post.Height * scale);
                DetailImage.Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform;
            }
            else if (viewportWidth > 0 && viewportHeight > 0)
            {
                DetailImage.MaxWidth = viewportWidth;
                DetailImage.MaxHeight = viewportHeight;
                DetailImage.Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform;
            }

            ImageScrollViewer.ChangeView(0, 0, 1.0f);
            FitButtonText.Text = "Actual size";
            FitIcon.Glyph = "\uE73F";
        }
        else
        {
            DetailImage.ClearValue(FrameworkElement.WidthProperty);
            DetailImage.ClearValue(FrameworkElement.HeightProperty);
            DetailImage.ClearValue(FrameworkElement.MaxWidthProperty);
            DetailImage.ClearValue(FrameworkElement.MaxHeightProperty);
            DetailImage.Stretch = Microsoft.UI.Xaml.Media.Stretch.None;

            FitButtonText.Text = "Fit to view";
            FitIcon.Glyph = "\uE740";
        }
    }

    private void FitToView_Click(object sender, RoutedEventArgs e)
    {
        _isFitToFill = !_isFitToFill;
        ApplyFitState();
    }

    private void ImageScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var wheelDelta = e.GetCurrentPoint(ImageScrollViewer).Properties.MouseWheelDelta;
        if (wheelDelta == 0) return;

        var zoomFactor = wheelDelta > 0 ? 1.15f : (1.0f / 1.15f);
        var targetZoom = Math.Clamp(ImageScrollViewer.ZoomFactor * zoomFactor, 1.0f, (float)ImageScrollViewer.MaxZoomFactor);
        var mousePos = e.GetCurrentPoint(ImageScrollViewer).Position;

        var targetHOffset = (ImageScrollViewer.HorizontalOffset + mousePos.X) * zoomFactor - mousePos.X;
        var targetVOffset = (ImageScrollViewer.VerticalOffset + mousePos.Y) * zoomFactor - mousePos.Y;

        ImageScrollViewer.ChangeView(targetHOffset, targetVOffset, targetZoom, disableAnimation: false);
        e.Handled = true;
    }

    private void ImageScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(ImageScrollViewer).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastDragPosition = e.GetCurrentPoint(ImageScrollViewer).Position;
            ImageScrollViewer.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void ImageScrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;

        var currentPoint = e.GetCurrentPoint(ImageScrollViewer).Position;
        var deltaX = _lastDragPosition.X - currentPoint.X;
        var deltaY = _lastDragPosition.Y - currentPoint.Y;
        _lastDragPosition = currentPoint;

        var targetHOffset = ImageScrollViewer.HorizontalOffset + deltaX;
        var targetVOffset = ImageScrollViewer.VerticalOffset + deltaY;

        ImageScrollViewer.ChangeView(targetHOffset, targetVOffset, null, disableAnimation: true);
        e.Handled = true;
    }

    private void ImageScrollViewer_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ImageScrollViewer.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    private void ImageScrollViewer_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
    }
}
