import 'package:fluent_ui/fluent_ui.dart';
import 'package:provider/provider.dart';
import '../services/tab_service.dart';
import '../viewmodels/saved_searches_viewmodel.dart';

class SavedSearchesView extends StatefulWidget {
  const SavedSearchesView({super.key});

  @override
  State<SavedSearchesView> createState() => _SavedSearchesViewState();
}

class _SavedSearchesViewState extends State<SavedSearchesView> {
  late final SavedSearchesViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = SavedSearchesViewModel();
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    return ChangeNotifierProvider<SavedSearchesViewModel>.value(
      value: _viewModel,
      child: Consumer<SavedSearchesViewModel>(
        builder: (context, vm, _) {
          return ScaffoldPage(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
            header: Row(
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Row(
                      children: [
                        Icon(FluentIcons.save, size: 20),
                        SizedBox(width: 8),
                        Text(
                          'Saved Searches',
                          style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Quickly revisit your bookmarked searches and preview recent posts',
                      style: TextStyle(fontSize: 13, color: theme.resources.textFillColorSecondary),
                    ),
                  ],
                ),
                const Spacer(),
                Button(
                  onPressed: () => vm.refreshAll(),
                  child: const Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(FluentIcons.refresh, size: 12),
                      SizedBox(width: 6),
                      Text('Refresh all'),
                    ],
                  ),
                ),
              ],
            ),
            content: vm.rows.isEmpty
                ? Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(FluentIcons.save, size: 40, color: theme.resources.textFillColorTertiary),
                        const SizedBox(height: 12),
                        Text(
                          'No saved searches yet.',
                          style: TextStyle(fontSize: 16, color: theme.resources.textFillColorSecondary),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          'Click the save icon next to the search bar to save searches here.',
                          style: TextStyle(fontSize: 12, color: theme.resources.textFillColorTertiary),
                        ),
                      ],
                    ),
                  )
                : ListView.builder(
                    primary: false,
                    itemCount: vm.rows.length,
                    itemBuilder: (context, index) {
                      final row = vm.rows[index];
                      final entry = row.entry;

                      return Padding(
                        padding: const EdgeInsets.only(bottom: 14),
                        child: Card(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Text(
                                          entry.displayTitle,
                                          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                                        ),
                                        const SizedBox(height: 2),
                                        Text(
                                          entry.effectiveQuery,
                                          style: TextStyle(fontSize: 12, color: theme.resources.textFillColorSecondary),
                                          maxLines: 1,
                                          overflow: TextOverflow.ellipsis,
                                        ),
                                      ],
                                    ),
                                  ),
                                  if (entry.cachedCount != null) ...[
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                      decoration: BoxDecoration(
                                        color: theme.resources.subtleFillColorSecondary,
                                        borderRadius: BorderRadius.circular(4),
                                      ),
                                      child: Text(
                                        '${entry.cachedCount} posts',
                                        style: TextStyle(fontSize: 11, color: theme.resources.textFillColorSecondary),
                                      ),
                                    ),
                                    const SizedBox(width: 8),
                                  ],
                                  Button(
                                    child: const Row(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        Icon(FluentIcons.search, size: 12),
                                        SizedBox(width: 6),
                                        Text('Open search'),
                                      ],
                                    ),
                                    onPressed: () {
                                      TabService.instance.openSearchTab(entry.effectiveQuery, entry.displayTitle);
                                    },
                                  ),
                                  const SizedBox(width: 6),
                                  IconButton(
                                    icon: const Icon(FluentIcons.delete, size: 12),
                                    onPressed: () => vm.removeSavedSearch(entry.id),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 12),

                              // Previews row
                              if (row.isLoading)
                                const SizedBox(
                                  height: 120,
                                  child: Center(child: ProgressRing()),
                                )
                              else if (row.previewPosts.isNotEmpty)
                                SizedBox(
                                  height: 120,
                                  child: ListView.separated(
                                    scrollDirection: Axis.horizontal,
                                    itemCount: row.previewPosts.length,
                                    separatorBuilder: (_, _) => const SizedBox(width: 10),
                                    itemBuilder: (context, pIdx) {
                                      final post = row.previewPosts[pIdx];
                                      return HoverButton(
                                        onPressed: () {
                                          TabService.instance.openPostTab(post);
                                        },
                                        builder: (context, states) {
                                          return Container(
                                            width: 120,
                                            height: 120,
                                            decoration: BoxDecoration(
                                              borderRadius: BorderRadius.circular(6),
                                              border: Border.all(
                                                color: states.contains(WidgetState.hovered) ? theme.accentColor : theme.resources.cardStrokeColorDefault,
                                              ),
                                            ),
                                            child: ClipRRect(
                                              borderRadius: BorderRadius.circular(5),
                                              child: post.previewUrl.isNotEmpty
                                                  ? Image.network(
                                                      post.previewUrl,
                                                      fit: BoxFit.cover,
                                                      headers: const {
                                                        'User-Agent': 'View621/1.0 (by View621_user on e621)',
                                                      },
                                                      errorBuilder: (_, _, _) => const Center(
                                                        child: Icon(FluentIcons.photo2, size: 24),
                                                      ),
                                                    )
                                                  : const Center(child: Icon(FluentIcons.photo2, size: 24)),
                                            ),
                                          );
                                        },
                                      );
                                    },
                                  ),
                                )
                              else
                                Text(
                                  'No preview available',
                                  style: TextStyle(fontSize: 12, color: theme.resources.textFillColorTertiary),
                                ),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
          );
        },
      ),
    );
  }
}
