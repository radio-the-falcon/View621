import 'package:fluent_ui/fluent_ui.dart';
import 'package:provider/provider.dart';
import '../viewmodels/favourites_viewmodel.dart';
import '../viewmodels/main_viewmodel.dart';
import '../widgets/post_card.dart';

class FavouritesView extends StatefulWidget {
  const FavouritesView({super.key});

  @override
  State<FavouritesView> createState() => _FavouritesViewState();
}

class _FavouritesViewState extends State<FavouritesView> {
  late final FavouritesViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = FavouritesViewModel();
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = FluentTheme.of(context);

    return ChangeNotifierProvider<FavouritesViewModel>.value(
      value: _viewModel,
      child: Consumer<FavouritesViewModel>(
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
                        Icon(FluentIcons.heart, size: 20),
                        SizedBox(width: 8),
                        Text(
                          'Favourites',
                          style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Your favourited posts on e621',
                      style: TextStyle(fontSize: 13, color: theme.resources.textFillColorSecondary),
                    ),
                  ],
                ),
                const Spacer(),
                if (vm.isLoggedIn)
                  Button(
                    onPressed: () => vm.loadFavorites(),
                    child: const Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(FluentIcons.refresh, size: 12),
                        SizedBox(width: 6),
                        Text('Refresh'),
                      ],
                    ),
                  ),
              ],
            ),
            content: !vm.isLoggedIn
                ? Center(
                    child: Card(
                      child: Padding(
                        padding: const EdgeInsets.all(24),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(FluentIcons.contact, size: 48),
                            const SizedBox(height: 14),
                            const Text(
                              'Account Login Required',
                              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              'Provide your e621 username and API key in Settings to view and synchronise your favourites.',
                              style: TextStyle(fontSize: 13, color: theme.resources.textFillColorSecondary),
                              textAlign: TextAlign.center,
                            ),
                            const SizedBox(height: 16),
                            FilledButton(
                              child: const Text('Open Settings'),
                              onPressed: () {
                                Provider.of<MainViewModel>(context, listen: false).navigateTo(TopNavDestination.settings);
                              },
                            ),
                          ],
                        ),
                      ),
                    ),
                  )
                : vm.isLoading
                    ? const Center(child: ProgressRing())
                    : vm.posts.isEmpty
                        ? Center(
                            child: Text(
                              vm.statusMessage.isNotEmpty ? vm.statusMessage : 'No favourited posts found.',
                              style: TextStyle(fontSize: 14, color: theme.resources.textFillColorSecondary),
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
