import 'package:fluent_ui/fluent_ui.dart';
import 'package:provider/provider.dart';
import '../viewmodels/pin_viewmodel.dart';
import '../widgets/post_card.dart';

class PinView extends StatefulWidget {
  const PinView({super.key});

  @override
  State<PinView> createState() => _PinViewState();
}

class _PinViewState extends State<PinView> {
  late final PinViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = PinViewModel();
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  void _confirmClearAll() {
    showDialog(
      context: context,
      builder: (context) => ContentDialog(
        title: const Text('Clear all pinned posts?'),
        content: const Text('Are you sure you want to remove all pinned posts? This will also clear cached sample images.'),
        actions: [
          Button(
            child: const Text('Cancel'),
            onPressed: () => Navigator.pop(context),
          ),
          FilledButton(
            child: const Text('Clear all'),
            onPressed: () {
              Navigator.pop(context);
              _viewModel.clearAll();
            },
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    return ChangeNotifierProvider<PinViewModel>.value(
      value: _viewModel,
      child: Consumer<PinViewModel>(
        builder: (context, vm, _) {
          return ScaffoldPage(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
            header: Row(
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Icon(FluentIcons.pin, size: 20),
                        const SizedBox(width: 8),
                        const Text(
                          'Pinned',
                          style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                        ),
                        if (vm.count > 0) ...[
                          const SizedBox(width: 10),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                            decoration: BoxDecoration(
                              color: theme.accentColor,
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: Text(
                              '${vm.count}',
                              style: const TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.bold),
                            ),
                          ),
                        ],
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Saved posts from your datastore',
                      style: TextStyle(fontSize: 13, color: theme.resources.textFillColorSecondary),
                    ),
                  ],
                ),
                const Spacer(),
                Button(
                  child: const Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(FluentIcons.refresh, size: 12),
                      SizedBox(width: 6),
                      Text('Refresh'),
                    ],
                  ),
                  onPressed: () => vm.loadBookmarkedPosts(),
                ),
                if (vm.count > 0) ...[
                  const SizedBox(width: 8),
                  Button(
                    onPressed: _confirmClearAll,
                    child: const Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(FluentIcons.delete, size: 12),
                        SizedBox(width: 6),
                        Text('Clear all'),
                      ],
                    ),
                  ),
                ],
              ],
            ),
            content: vm.isLoading
                ? const Center(child: ProgressRing())
                : vm.posts.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(FluentIcons.pin, size: 40, color: theme.resources.textFillColorTertiary),
                            const SizedBox(height: 12),
                            Text(
                              'No pinned posts yet.',
                              style: TextStyle(fontSize: 16, color: theme.resources.textFillColorSecondary),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              'Click the pin icon on any thumbnail or post view to save posts here.',
                              style: TextStyle(fontSize: 12, color: theme.resources.textFillColorTertiary),
                            ),
                          ],
                        ),
                      )
                    : LayoutBuilder(
                        builder: (context, constraints) {
                          const double minCardWidth = 220.0;
                          final int crossAxisCount = (constraints.maxWidth / (minCardWidth + 12)).floor().clamp(1, 12);
                          const double cardHeight = 285.0;

                          return GridView.builder(
                            primary: false,
                            padding: const EdgeInsets.only(top: 8, bottom: 16),
                            gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                              crossAxisCount: crossAxisCount,
                              crossAxisSpacing: 12,
                              mainAxisSpacing: 12,
                              mainAxisExtent: cardHeight,
                            ),
                            itemCount: vm.posts.length,
                            itemBuilder: (context, index) {
                              final post = vm.posts[index];
                              return PostCard(
                                key: ValueKey(post.id),
                                post: post,
                                parentList: vm.posts,
                                indexInList: index,
                              );
                            },
                          );
                        },
                      ),
          );
        },
      ),
    );
  }
}
