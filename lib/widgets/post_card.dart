import 'dart:io';
import 'package:fluent_ui/fluent_ui.dart';
import 'package:flutter/services.dart';
import '../models/image_post.dart';
import '../models/post_navigation_context.dart';
import '../services/bookmark_service.dart';
import '../services/tab_service.dart';

class PostCard extends StatelessWidget {
  final ImagePost post;
  final List<ImagePost>? parentList;
  final int? indexInList;

  const PostCard({
    super.key,
    required this.post,
    this.parentList,
    this.indexInList,
  });

  void _openPost([bool inNewTab = false]) {
    final navContext = (parentList != null && indexInList != null)
        ? PostNavigationContext(
            mode: PostNavigationMode.searchResults,
            posts: parentList,
            currentIndex: indexInList!,
          )
        : null;

    if (inNewTab) {
      TabService.instance.openPostTab(post, navContext);
    } else {
      TabService.instance.openPostTab(post, navContext);
    }
  }

  void _toggleBookmark() {
    BookmarkService.instance.toggleBookmark(post.id, post);
  }

  void _copyImageLink() {
    final link = post.fullUrl.isNotEmpty ? post.fullUrl : post.sampleUrl;
    if (link.isNotEmpty) {
      Clipboard.setData(ClipboardData(text: link));
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    final isPinned = post.isBookmarked;
    final pinBgColor = isPinned
        ? const Color(0xFFFFB800)
        : const Color.fromRGBO(30, 30, 30, 0.78);
    final pinBorderColor = isPinned
        ? const Color(0xFFF59E0B)
        : const Color.fromRGBO(255, 255, 255, 0.3);
    final pinIconColor = isPinned
        ? const Color(0xFF141414)
        : Colors.white;

    final localPath = post.effectiveSamplePath;
    final hasLocal = localPath != null && File(localPath).existsSync();

    return GestureDetector(
      onSecondaryTapUp: (details) {
        // Context menu on right click
        _showContextMenu(context, details.globalPosition);
      },
      onTertiaryTapUp: (_) {
        // Middle click opens in new tab
        _openPost(true);
      },
      child: HoverButton(
        onPressed: () => _openPost(false),
        builder: (context, states) {
          return Container(
            height: 285,
            decoration: BoxDecoration(
              color: theme.cardColor,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(
                color: states.contains(WidgetState.hovered)
                    ? theme.accentColor.withValues(alpha: 0.6)
                    : theme.resources.cardStrokeColorDefault,
                width: 1,
              ),
              boxShadow: states.contains(WidgetState.hovered)
                  ? [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: isDark ? 0.4 : 0.08),
                        blurRadius: 8,
                        offset: const Offset(0, 4),
                      )
                    ]
                  : null,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Thumbnail area
                SizedBox(
                  height: 185,
                  child: Stack(
                    fit: StackFit.expand,
                    children: [
                      ClipRRect(
                        borderRadius: const BorderRadius.vertical(top: Radius.circular(7)),
                        child: Container(
                          color: isDark ? const Color(0xFF1E1E1E) : const Color(0xFFEEEEEE),
                          child: hasLocal
                              ? Image.file(
                                  File(localPath),
                                  fit: BoxFit.cover,
                                  errorBuilder: (_, _, _) => _buildFallback(),
                                )
                              : (post.previewUrl.isNotEmpty
                                  ? Image.network(
                                      post.previewUrl,
                                      fit: BoxFit.cover,
                                      headers: const {
                                        'User-Agent': 'View621/1.0 (by View621_user on e621)',
                                      },
                                      loadingBuilder: (_, child, progress) {
                                        if (progress == null) return child;
                                        return const Center(child: ProgressRing(strokeWidth: 2.5));
                                      },
                                      errorBuilder: (_, _, _) => _buildFallback(),
                                    )
                                  : _buildFallback()),
                        ),
                      ),

                      // Pin / Bookmark Button overlay in top-right
                      Positioned(
                        top: 6,
                        right: 6,
                        child: Tooltip(
                          message: isPinned ? 'Unpin post' : 'Pin post',
                          child: IconButton(
                            icon: Container(
                              width: 30,
                              height: 30,
                              decoration: BoxDecoration(
                                color: pinBgColor,
                                shape: BoxShape.circle,
                                border: Border.all(color: pinBorderColor, width: 1),
                              ),
                              child: Center(
                                child: Icon(
                                  isPinned ? FluentIcons.pinned_solid : FluentIcons.pin,
                                  size: 13,
                                  color: pinIconColor,
                                ),
                              ),
                            ),
                            onPressed: _toggleBookmark,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),

                // Card Footer
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        // Top row: Title and format/variation badges
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                post.displayName,
                                style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                            const SizedBox(width: 4),

                            // Media Type Badge
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                              decoration: BoxDecoration(
                                color: theme.resources.subtleFillColorSecondary,
                                borderRadius: BorderRadius.circular(4),
                                border: Border.all(color: theme.resources.cardStrokeColorDefault),
                              ),
                              child: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Icon(
                                    post.isVideo
                                        ? FluentIcons.video
                                        : (post.isAnimatedGif ? FluentIcons.gif : FluentIcons.photo2),
                                    size: 9,
                                    color: theme.resources.textFillColorSecondary,
                                  ),
                                  const SizedBox(width: 3),
                                  Text(
                                    post.mediaTypeLabel,
                                    style: TextStyle(
                                      fontSize: 9,
                                      fontWeight: FontWeight.w500,
                                      color: theme.resources.textFillColorSecondary,
                                    ),
                                  ),
                                ],
                              ),
                            ),

                            // Pool Badge (P)
                            if (post.isInPool) ...[
                              const SizedBox(width: 3),
                              Container(
                                width: 17,
                                height: 17,
                                decoration: BoxDecoration(
                                  color: isDark ? const Color(0xFF162A3F) : const Color(0xFFE3F2FD),
                                  borderRadius: BorderRadius.circular(4),
                                  border: Border.all(
                                    color: isDark ? const Color(0xFF234363) : const Color(0xFFBBDEFB),
                                  ),
                                ),
                                child: Center(
                                  child: Text(
                                    'P',
                                    style: TextStyle(
                                      fontSize: 10,
                                      fontWeight: FontWeight.bold,
                                      color: isDark ? const Color(0xFF64B5F6) : const Color(0xFF1565C0),
                                    ),
                                  ),
                                ),
                              ),
                            ],

                            // Children Badge (C)
                            if (post.hasChildren) ...[
                              const SizedBox(width: 3),
                              Container(
                                width: 17,
                                height: 17,
                                decoration: BoxDecoration(
                                  color: isDark ? const Color(0xFF1B2E21) : const Color(0xFFE8F5E9),
                                  borderRadius: BorderRadius.circular(4),
                                  border: Border.all(
                                    color: isDark ? const Color(0xFF2A4533) : const Color(0xFFC8E6C9),
                                  ),
                                ),
                                child: Center(
                                  child: Text(
                                    'C',
                                    style: TextStyle(
                                      fontSize: 10,
                                      fontWeight: FontWeight.bold,
                                      color: isDark ? const Color(0xFF81C784) : const Color(0xFF2E7D32),
                                    ),
                                  ),
                                ),
                              ),
                            ],
                          ],
                        ),

                        // Middle row: Artist and dimensions
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Expanded(
                              child: Text(
                                post.artist,
                                style: TextStyle(
                                  fontSize: 11,
                                  color: theme.resources.textFillColorSecondary,
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                            Text(
                              post.dimensionText,
                              style: TextStyle(
                                fontSize: 10,
                                color: theme.resources.textFillColorTertiary,
                              ),
                            ),
                          ],
                        ),

                        // Bottom row: Rating, Score, Favourites
                        Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                              decoration: BoxDecoration(
                                color: theme.resources.subtleFillColorSecondary,
                                borderRadius: BorderRadius.circular(4),
                                border: Border.all(color: theme.resources.cardStrokeColorDefault),
                              ),
                              child: Text(
                                post.rating,
                                style: TextStyle(
                                  fontSize: 10,
                                  fontWeight: FontWeight.w600,
                                  color: theme.resources.textFillColorPrimary,
                                ),
                              ),
                            ),
                            const SizedBox(width: 8),
                            Text(
                              post.scoreText,
                              style: TextStyle(
                                fontSize: 10,
                                color: theme.resources.textFillColorSecondary,
                              ),
                            ),
                            const SizedBox(width: 8),
                            Text(
                              post.favouritesText,
                              style: TextStyle(
                                fontSize: 10,
                                color: theme.resources.textFillColorTertiary,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildFallback() {
    return const Center(
      child: Icon(FluentIcons.photo2, size: 28, color: Colors.grey),
    );
  }

  void _showContextMenu(BuildContext context, Offset position) {
    showDialog(
      context: context,
      barrierColor: Colors.transparent,
      builder: (ctx) {
        return Stack(
          children: [
            Positioned.fill(
              child: GestureDetector(
                behavior: HitTestBehavior.opaque,
                onTap: () => Navigator.pop(ctx),
              ),
            ),
            Positioned(
              left: position.dx,
              top: position.dy,
              child: FlyoutContent(
                child: MenuFlyout(
                  items: [
                    MenuFlyoutItem(
                      leading: const Icon(FluentIcons.open_in_new_window),
                      text: const Text('Open in new tab'),
                      onPressed: () {
                        Navigator.pop(ctx);
                        _openPost(true);
                      },
                    ),
                    MenuFlyoutItem(
                      leading: const Icon(FluentIcons.copy),
                      text: const Text('Copy image link'),
                      onPressed: () {
                        Navigator.pop(ctx);
                        _copyImageLink();
                      },
                    ),
                    const MenuFlyoutSeparator(),
                    MenuFlyoutItem(
                      leading: Icon(post.isBookmarked ? FluentIcons.pinned_solid : FluentIcons.pin),
                      text: Text(post.isBookmarked ? 'Unpin post' : 'Pin post'),
                      onPressed: () {
                        Navigator.pop(ctx);
                        _toggleBookmark();
                      },
                    ),
                  ],
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}
