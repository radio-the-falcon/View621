import 'package:fluent_ui/fluent_ui.dart';
import 'package:provider/provider.dart';
import '../services/saved_searches_service.dart';
import '../viewmodels/search_viewmodel.dart';
import '../widgets/post_card.dart';

class SearchView extends StatefulWidget {
  final String? initialQuery;

  const SearchView({super.key, this.initialQuery});

  @override
  State<SearchView> createState() => _SearchViewState();
}

class _SearchViewState extends State<SearchView> {
  late final SearchViewModel _viewModel;
  late final TextEditingController _textController;

  @override
  void initState() {
    super.initState();
    _viewModel = SearchViewModel(initialQuery: widget.initialQuery);
    _textController = TextEditingController(text: widget.initialQuery ?? '');
  }

  @override
  void dispose() {
    _textController.dispose();
    _viewModel.dispose();
    super.dispose();
  }

  void _onTagChipClicked(String tag) {
    final current = _textController.text.trim();
    if (current.isEmpty) {
      _textController.text = tag;
    } else if (!current.contains(tag)) {
      _textController.text = '$current $tag';
    }
    _viewModel.searchQuery = _textController.text;
    _viewModel.search(resetPage: true);
  }

  void _saveCurrentSearch() {
    final effectiveQuery = _viewModel.buildEffectiveQuery();
    final display = _viewModel.searchQuery.trim().isNotEmpty
        ? _viewModel.searchQuery.trim()
        : 'All Posts';

    SavedSearchesService.instance.saveSearch(
      display,
      effectiveQuery,
      sortKey: _viewModel.sortKey,
      isSortReversed: _viewModel.isSortReversed,
      cachedCount: _viewModel.posts.length,
    );

    displayInfoBar(
      context,
      builder: (context, close) {
        return InfoBar(
          title: const Text('Search saved'),
          content: Text('"$display" added to Saved Searches.'),
          severity: InfoBarSeverity.success,
          onClose: close,
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    return ChangeNotifierProvider<SearchViewModel>.value(
      value: _viewModel,
      child: Consumer<SearchViewModel>(
        builder: (context, vm, _) {
          return ScaffoldPage(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
            header: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Search bar row
                Row(
                  children: [
                    // Refresh button
                    Tooltip(
                      message: 'Refresh search results',
                      child: IconButton(
                        icon: const Icon(FluentIcons.refresh, size: 14),
                        onPressed: () => vm.search(resetPage: false, forceRefresh: true),
                      ),
                    ),
                    const SizedBox(width: 8),

                    // Search box with autocomplete
                    Expanded(
                      child: AutoSuggestBox<String>(
                        controller: _textController,
                        placeholder: 'Search tags (e.g. canine rating:s)...',
                        items: vm.tagSuggestions.map((tag) {
                          return AutoSuggestBoxItem<String>(
                            value: tag,
                            label: tag,
                          );
                        }).toList(),
                        onChanged: (text, reason) {
                          if (reason == TextChangedReason.userInput) {
                            vm.onQueryChanged(text);
                          }
                        },
                        onSelected: (item) {
                          vm.selectSuggestion(item.value ?? '');
                          _textController.text = vm.searchQuery;
                          vm.search(resetPage: true);
                        },
                      ),
                    ),
                    const SizedBox(width: 8),

                    // Search button (Accent style)
                    FilledButton(
                      child: const Padding(
                        padding: EdgeInsets.symmetric(horizontal: 4),
                        child: Icon(FluentIcons.search, size: 14),
                      ),
                      onPressed: () {
                        vm.searchQuery = _textController.text;
                        vm.search(resetPage: true);
                      },
                    ),
                    const SizedBox(width: 8),

                    // Sort menu
                    DropDownButton(
                      title: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(FluentIcons.sort, size: 14),
                          const SizedBox(width: 8),
                          Text(vm.sortLabel),
                        ],
                      ),
                      items: [
                        MenuFlyoutItem(
                          text: const Text('Default (Newest ID first)'),
                          trailing: vm.sortKey == 'id' && !vm.isSortReversed ? const Icon(FluentIcons.check_mark, size: 12) : null,
                          onPressed: () => vm.setSort('id'),
                        ),
                        MenuFlyoutItem(
                          text: const Text('Date created (Newest first)'),
                          trailing: vm.sortKey == 'created' && !vm.isSortReversed ? const Icon(FluentIcons.check_mark, size: 12) : null,
                          onPressed: () => vm.setSort('created'),
                        ),
                        MenuFlyoutItem(
                          text: const Text('Score (Highest first)'),
                          trailing: vm.sortKey == 'score' && !vm.isSortReversed ? const Icon(FluentIcons.check_mark, size: 12) : null,
                          onPressed: () => vm.setSort('score'),
                        ),
                        MenuFlyoutItem(
                          text: const Text('Favourites (Most first)'),
                          trailing: vm.sortKey == 'favcount' && !vm.isSortReversed ? const Icon(FluentIcons.check_mark, size: 12) : null,
                          onPressed: () => vm.setSort('favcount'),
                        ),
                        const MenuFlyoutSeparator(),
                        MenuFlyoutItem(
                          text: const Text('Reverse order (Ascending)'),
                          trailing: vm.isSortReversed ? const Icon(FluentIcons.check_mark, size: 12) : null,
                          onPressed: () => vm.setSortReversed(!vm.isSortReversed),
                        ),
                      ],
                    ),
                    const SizedBox(width: 8),

                    // Save search button
                    Tooltip(
                      message: 'Save this search',
                      child: IconButton(
                        icon: const Icon(FluentIcons.save, size: 14),
                        onPressed: _saveCurrentSearch,
                      ),
                    ),
                  ],
                ),

                const SizedBox(height: 12),

                // Quick filter chips
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: [
                      Text(
                        'Quick filters:',
                        style: TextStyle(
                          fontSize: 12,
                          color: theme.resources.textFillColorSecondary,
                        ),
                      ),
                      const SizedBox(width: 8),
                      for (final tag in const ['order:score', 'rating:s', 'scenery', 'canine', 'feline', 'dragon']) ...[
                        Button(
                          style: ButtonStyle(
                            shape: WidgetStateProperty.all(RoundedRectangleBorder(borderRadius: BorderRadius.circular(12))),
                            padding: WidgetStateProperty.all(const EdgeInsets.symmetric(horizontal: 10, vertical: 3)),
                          ),
                          onPressed: () => _onTagChipClicked(tag),
                          child: Text(tag, style: const TextStyle(fontSize: 12)),
                        ),
                        const SizedBox(width: 6),
                      ],
                    ],
                  ),
                ),
              ],
            ),
            content: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Expanded(
                  child: Stack(
                    children: [
                      if (vm.isLoading)
                        Center(
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              const ProgressRing(strokeWidth: 3),
                              const SizedBox(height: 14),
                              Text(
                                'Loading images from provider...',
                                style: TextStyle(color: theme.resources.textFillColorSecondary),
                              ),
                            ],
                          ),
                        )
                      else if (vm.posts.isEmpty)
                        Center(
                          child: Text(
                            vm.statusMessage.isNotEmpty ? vm.statusMessage : 'No posts found.',
                            style: TextStyle(fontSize: 14, color: theme.resources.textFillColorSecondary),
                          ),
                        )
                      else
                        LayoutBuilder(
                          builder: (context, constraints) {
                            const double minCardWidth = 220.0;
                            final int crossAxisCount = (constraints.maxWidth / (minCardWidth + 12)).floor().clamp(1, 12);
                            const double cardHeight = 285.0;

                            return GridView.builder(
                              primary: false,
                              padding: const EdgeInsets.only(top: 8, bottom: 8),
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
                    ],
                  ),
                ),

                // Pagination Bar
                Container(
                  padding: const EdgeInsets.only(top: 8),
                  child: Row(
                    children: [
                      Expanded(
                        child: Text(
                          vm.statusMessage,
                          style: TextStyle(fontSize: 12, color: theme.resources.textFillColorSecondary),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Button(
                        onPressed: vm.hasPreviousPage ? () => vm.previousPage() : null,
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(FluentIcons.chevron_left, size: 10),
                            SizedBox(width: 4),
                            Text('Previous'),
                          ],
                        ),
                      ),
                      const SizedBox(width: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: theme.resources.subtleFillColorSecondary,
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: Text(
                          '${vm.currentPage}',
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Button(
                        onPressed: vm.hasNextPage ? () => vm.nextPage() : null,
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text('Next'),
                            SizedBox(width: 4),
                            Icon(FluentIcons.chevron_right, size: 10),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
