import 'dart:io';
import 'package:fluent_ui/fluent_ui.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import '../models/image_post.dart';
import '../models/post_navigation_context.dart';
import '../services/app_settings.dart';
import '../services/tab_service.dart';
import '../viewmodels/post_viewmodel.dart';
import '../widgets/video_player_widget.dart';

class PostView extends StatefulWidget {
  final ImagePost post;
  final PostNavigationContext? navigationContext;

  const PostView({
    super.key,
    required this.post,
    this.navigationContext,
  });

  @override
  State<PostView> createState() => _PostViewState();
}

class _PostViewState extends State<PostView> {
  late final PostViewModel _viewModel;
  final TransformationController _transformController = TransformationController();
  final FocusNode _focusNode = FocusNode();

  @override
  void initState() {
    super.initState();
    _viewModel = PostViewModel(
      post: widget.post,
      navigationContext: widget.navigationContext,
    );
  }

  @override
  void dispose() {
    _focusNode.dispose();
    _transformController.dispose();
    _viewModel.dispose();
    super.dispose();
  }

  void _handleKeyEvent(KeyEvent event) {
    if (event is KeyDownEvent) {
      if (event.logicalKey == LogicalKeyboardKey.arrowLeft) {
        if (_viewModel.canGoPrevious) {
          _viewModel.goToPrevious();
          _transformController.value = Matrix4.identity();
        }
      } else if (event.logicalKey == LogicalKeyboardKey.arrowRight) {
        if (_viewModel.canGoNext) {
          _viewModel.goToNext();
          _transformController.value = Matrix4.identity();
        }
      }
    }
  }

  void _openInBrowser() {
    final url = 'https://e621.net/posts/${_viewModel.post.id}';
    launchUrl(Uri.parse(url), mode: LaunchMode.externalApplication);
  }

  void _copyUrl() {
    final link = _viewModel.post.fullUrl.isNotEmpty ? _viewModel.post.fullUrl : _viewModel.post.sampleUrl;
    if (link.isNotEmpty) {
      Clipboard.setData(ClipboardData(text: link));
      displayInfoBar(
        context,
        builder: (context, close) => InfoBar(
          title: const Text('URL Copied'),
          content: const Text('Post URL copied to clipboard.'),
          severity: InfoBarSeverity.info,
          onClose: close,
        ),
      );
    }
  }

  void _downloadFile() {
    final link = _viewModel.post.fullUrl.isNotEmpty ? _viewModel.post.fullUrl : _viewModel.post.sampleUrl;
    if (link.isNotEmpty) {
      launchUrl(Uri.parse(link), mode: LaunchMode.externalApplication);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<PostViewModel>.value(
      value: _viewModel,
      child: Consumer<PostViewModel>(
        builder: (context, vm, _) {
          final post = vm.post;

          return KeyboardListener(
            focusNode: _focusNode,
            autofocus: true,
            onKeyEvent: _handleKeyEvent,
            child: ScaffoldPage(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
              header: _buildActionBar(context, vm, post),
              content: LayoutBuilder(
                builder: (context, constraints) {
                  return Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      // Left: Zoomable Media Viewport
                      Expanded(
                        child: _buildMediaViewport(context, vm, post),
                      ),

                      const SizedBox(width: 14),

                      // Right: Metadata & tags sidebar (width 320px)
                      SizedBox(
                        width: 320,
                        child: _buildMetadataSidebar(context, vm, post),
                      ),
                    ],
                  );
                },
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildActionBar(BuildContext context, PostViewModel vm, ImagePost post) {
    return Container(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          // Back button
          Button(
            child: const Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(FluentIcons.chevron_left, size: 12),
                SizedBox(width: 4),
                Text('Back'),
              ],
            ),
            onPressed: () => TabService.instance.closeCurrentTab(),
          ),

          const SizedBox(width: 12),

          // Prev / Next Post buttons
          Button(
            onPressed: vm.canGoPrevious
                ? () {
                    vm.goToPrevious();
                    _transformController.value = Matrix4.identity();
                  }
                : null,
            child: const Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(FluentIcons.previous, size: 11),
                SizedBox(width: 4),
                Text('Prev'),
              ],
            ),
          ),
          const SizedBox(width: 4),
          Button(
            onPressed: vm.canGoNext
                ? () {
                    vm.goToNext();
                    _transformController.value = Matrix4.identity();
                  }
                : null,
            child: const Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text('Next'),
                SizedBox(width: 4),
                Icon(FluentIcons.next, size: 11),
              ],
            ),
          ),

          const Spacer(),

          // Toggle resolution button
          if (!post.isVideo)
            FilledButton(
              onPressed: () {
                vm.toggleResolution();
                _transformController.value = Matrix4.identity();
              },
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(FluentIcons.view, size: 14),
                  const SizedBox(width: 6),
                  Text(vm.isOriginalView ? 'Switch to sample view' : 'Switch to full view'),
                ],
              ),
            ),

          const SizedBox(width: 8),

          // Bookmark Button
          Tooltip(
            message: post.isBookmarked ? 'Unpin post' : 'Pin post',
            child: IconButton(
              icon: Icon(
                post.isBookmarked ? FluentIcons.pinned_solid : FluentIcons.pin,
                size: 14,
                color: post.isBookmarked ? const Color(0xFFFFB800) : null,
              ),
              onPressed: () => vm.toggleBookmark(),
            ),
          ),

          const SizedBox(width: 4),

          // Open in Browser
          Tooltip(
            message: 'Open post on website',
            child: IconButton(
              icon: const Icon(FluentIcons.globe, size: 14),
              onPressed: _openInBrowser,
            ),
          ),

          const SizedBox(width: 4),

          // Copy URL
          Tooltip(
            message: 'Copy URL',
            child: IconButton(
              icon: const Icon(FluentIcons.copy, size: 14),
              onPressed: _copyUrl,
            ),
          ),

          const SizedBox(width: 4),

          // Download full file
          Tooltip(
            message: 'Download full file',
            child: IconButton(
              icon: const Icon(FluentIcons.download, size: 14),
              onPressed: _downloadFile,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMediaViewport(BuildContext context, PostViewModel vm, ImagePost post) {
    final theme = FluentTheme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    final localPath = post.effectiveSamplePath;
    final hasLocal = localPath != null && File(localPath).existsSync();

    final mediaUrl = vm.isOriginalView
        ? (post.fullUrl.isNotEmpty ? post.fullUrl : post.sampleUrl)
        : (post.sampleUrl.isNotEmpty ? post.sampleUrl : post.fullUrl);

    return Container(
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF141414) : const Color(0xFFEBEBEB),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: theme.resources.cardStrokeColorDefault),
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(7),
        child: Stack(
          fit: StackFit.expand,
          children: [
            if (post.isVideo)
              VideoPlayerWidget(videoUrl: mediaUrl)
            else
              InteractiveViewer(
                transformationController: _transformController,
                minScale: 0.1,
                maxScale: 6.0,
                child: Center(
                  child: hasLocal && !vm.isOriginalView
                      ? Image.file(
                          File(localPath),
                          fit: vm.isFitToFill ? BoxFit.contain : BoxFit.none,
                          errorBuilder: (_, _, _) => const Icon(FluentIcons.error, size: 36),
                        )
                      : Image.network(
                          mediaUrl,
                          fit: vm.isFitToFill ? BoxFit.contain : BoxFit.none,
                          headers: const {
                            'User-Agent': 'View621/1.0 (by View621_user on e621)',
                          },
                          loadingBuilder: (_, child, progress) {
                            if (progress == null) return child;
                            return const Center(child: ProgressRing(strokeWidth: 3));
                          },
                          errorBuilder: (_, _, _) => const Icon(FluentIcons.error, size: 36),
                        ),
                ),
              ),

            // Floating Fit to View / Actual size button in top-right
            if (!post.isVideo)
              Positioned(
                top: 12,
                right: 12,
                child: Button(
                  onPressed: () {
                    vm.toggleFitToFill();
                    _transformController.value = Matrix4.identity();
                  },
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        vm.isFitToFill ? FluentIcons.full_screen : FluentIcons.fit_page,
                        size: 13,
                      ),
                      const SizedBox(width: 6),
                      Text(vm.isFitToFill ? 'Actual size' : 'Fit to view'),
                    ],
                  ),
                ),
              ),

            // Pool / Series Toolbar in top-left
            if (vm.context.mode == PostNavigationMode.pool)
              Positioned(
                top: 12,
                left: 12,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                  decoration: BoxDecoration(
                    color: theme.cardColor,
                    borderRadius: BorderRadius.circular(6),
                    border: Border.all(color: theme.resources.cardStrokeColorDefault),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(FluentIcons.library, size: 14, color: theme.accentColor),
                      const SizedBox(width: 8),
                      Text(
                        '${vm.context.poolName ?? 'Pool #${vm.context.poolId}'} (${vm.context.currentIndex + 1}/${vm.context.poolPostIds?.length ?? 0})',
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 12),
                      ),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildMetadataSidebar(BuildContext context, PostViewModel vm, ImagePost post) {
    final theme = FluentTheme.of(context);

    return SingleChildScrollView(
      primary: false,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Post Summary Card
          Card(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  post.displayName,
                  style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                ),
                const SizedBox(height: 2),
                Text(
                  post.artist,
                  style: TextStyle(fontSize: 13, color: theme.resources.textFillColorSecondary),
                ),
                const SizedBox(height: 8),
                const Divider(),
                const SizedBox(height: 8),
                _buildInfoRow('Post ID', '#${post.id}'),
                _buildInfoRow('Rating', post.rating),
                _buildInfoRow('Dimensions', post.dimensionText),
                _buildInfoRow('File Size', post.fileSizeFormatted),
                _buildInfoRow('Score', '${post.score}'),
                _buildInfoRow('Favourites', '${post.favCount}'),
              ],
            ),
          ),

          const SizedBox(height: 14),

          // Post Actions Card (Voting & Favourite)
          Card(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Post Actions',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    // Upvote
                    Expanded(
                      child: Tooltip(
                        message: 'Upvote (Score +1)',
                        child: Button(
                          onPressed: () => vm.vote(1),
                          child: Icon(
                            FluentIcons.like,
                            size: 16,
                            color: post.ourScore == 1 ? theme.accentColor : null,
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),

                    // Downvote
                    Expanded(
                      child: Tooltip(
                        message: 'Downvote (Score -1)',
                        child: Button(
                          onPressed: () => vm.vote(-1),
                          child: Icon(
                            FluentIcons.dislike,
                            size: 16,
                            color: post.ourScore == -1 ? Colors.red : null,
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),

                    // Favourite
                    Expanded(
                      child: Tooltip(
                        message: post.isFavorited ? 'Remove favourite' : 'Add to favourites',
                        child: Button(
                          onPressed: () => vm.toggleFavorite(),
                          child: Icon(
                            post.isFavorited ? FluentIcons.heart_fill : FluentIcons.heart,
                            size: 16,
                            color: post.isFavorited ? Colors.red : null,
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
                if (!AppSettings.instance.isLoggedIn) ...[
                  const SizedBox(height: 10),
                  const InfoBar(
                    title: Text('Login required'),
                    content: Text('Set username and API key in Settings to vote and favourite.'),
                    severity: InfoBarSeverity.info,
                  ),
                ],
              ],
            ),
          ),

          // Series and Relationships Card
          if (post.hasRelationships || post.isInPool) ...[
            const SizedBox(height: 14),
            Card(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Series & Variations',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                  ),
                  const SizedBox(height: 8),

                  // Parent post
                  if (post.hasParent) ...[
                    Text(
                      'Parent post:',
                      style: TextStyle(fontSize: 12, color: theme.resources.textFillColorSecondary),
                    ),
                    const SizedBox(height: 4),
                    Button(
                      child: Text('Post #${post.parentId}'),
                      onPressed: () {
                        TabService.instance.openPostTab(ImagePost(id: post.parentId!));
                      },
                    ),
                    const SizedBox(height: 8),
                  ],

                  // Pools
                  if (post.isInPool) ...[
                    Text(
                      'Pools & Comics:',
                      style: TextStyle(fontSize: 12, color: theme.resources.textFillColorSecondary),
                    ),
                    const SizedBox(height: 4),
                    for (final pool in vm.pools)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 4),
                        child: Button(
                          child: Row(
                            children: [
                              Icon(FluentIcons.library, size: 12, color: theme.accentColor),
                              const SizedBox(width: 6),
                              Expanded(
                                child: Text(pool.name, overflow: TextOverflow.ellipsis),
                              ),
                            ],
                          ),
                          onPressed: () {
                            TabService.instance.openPoolTab(pool.id, pool.name);
                          },
                        ),
                      ),
                    const SizedBox(height: 8),
                  ],

                  // Children
                  if (post.childIds.isNotEmpty) ...[
                    Text(
                      'Child variations:',
                      style: TextStyle(fontSize: 12, color: theme.resources.textFillColorSecondary),
                    ),
                    const SizedBox(height: 4),
                    Wrap(
                      spacing: 4,
                      runSpacing: 4,
                      children: [
                        for (final cid in post.childIds)
                          Button(
                            child: Text('#$cid'),
                            onPressed: () {
                              TabService.instance.openPostTab(ImagePost(id: cid));
                            },
                          ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          ],

          const SizedBox(height: 14),

          // Tags Card
          Card(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Tags',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                ),
                const SizedBox(height: 10),
                if (post.artistTags.isNotEmpty) ...[
                  Text(
                    'Artist',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12, color: theme.accentColor),
                  ),
                  const SizedBox(height: 4),
                  _buildTagGroup(post.artistTags, theme.accentColor),
                  const SizedBox(height: 8),
                ],
                if (post.characterTags.isNotEmpty) ...[
                  const Text(
                    'Characters',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12, color: Color(0xFF4CAF50)),
                  ),
                  const SizedBox(height: 4),
                  _buildTagGroup(post.characterTags, const Color(0xFF4CAF50)),
                  const SizedBox(height: 8),
                ],
                if (post.speciesTags.isNotEmpty) ...[
                  const Text(
                    'Species',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12, color: Color(0xFFFF9800)),
                  ),
                  const SizedBox(height: 4),
                  _buildTagGroup(post.speciesTags, const Color(0xFFFF9800)),
                  const SizedBox(height: 8),
                ],
                if (post.generalTags.isNotEmpty) ...[
                  Text(
                    'General',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12, color: theme.resources.textFillColorSecondary),
                  ),
                  const SizedBox(height: 4),
                  _buildTagGroup(post.generalTags, theme.resources.textFillColorSecondary),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTagGroup(List<String> tags, Color color) {
    return Wrap(
      spacing: 6,
      runSpacing: 6,
      children: [
        for (final tag in tags)
          Button(
            child: Text(tag, style: TextStyle(color: color, fontSize: 12)),
            onPressed: () {
              TabService.instance.openSearchTab(tag);
            },
          ),
      ],
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(fontSize: 12, color: Colors.grey)),
          Text(value, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}
