import 'package:fluent_ui/fluent_ui.dart';
import '../models/image_post.dart';
import '../models/post_navigation_context.dart';
import '../services/app_settings.dart';
import '../services/bookmark_service.dart';
import '../services/favorites_service.dart';
import '../services/saved_searches_service.dart';
import '../services/search_history_service.dart';
import '../services/tab_service.dart';
import '../views/about_view.dart';
import '../views/favourites_view.dart';
import '../views/pin_view.dart';
import '../views/post_view.dart';
import '../views/saved_searches_view.dart';
import '../views/search_view.dart';
import '../views/settings_view.dart';

enum TopNavDestination {
  explore,
  pinned,
  saved,
  favourites,
  about,
  settings,
}

class AppTabItem {
  final String id;
  String title;
  final IconData icon;
  final Widget content;

  AppTabItem({
    required this.id,
    required this.title,
    required this.icon,
    required this.content,
  });
}

class MainViewModel extends ChangeNotifier {
  TopNavDestination _navDestination = TopNavDestination.explore;
  final List<AppTabItem> _tabs = [];
  int _activeTabIndex = 0;
  double _searchHistoryHeight = 160.0;

  MainViewModel() {
    _initTabService();
    _initListeners();
    addSearchTab();
  }

  TopNavDestination get navDestination => _navDestination;
  List<AppTabItem> get tabs => List.unmodifiable(_tabs);
  int get activeTabIndex => _activeTabIndex;
  double get searchHistoryHeight => _searchHistoryHeight;

  set searchHistoryHeight(double val) {
    _searchHistoryHeight = val.clamp(64.0, 450.0);
    notifyListeners();
  }

  int get pinnedCount => BookmarkService.instance.bookmarkedPostIds.length;
  int get savedCount => SavedSearchesService.instance.savedSearches.length;
  int? get favouritesCount => AppSettings.instance.isLoggedIn ? FavoritesService.instance.cachedCount : null;

  void _initListeners() {
    BookmarkService.instance.addListener(notifyListeners);
    SavedSearchesService.instance.addListener(notifyListeners);
    FavoritesService.instance.addListener(notifyListeners);
    SearchHistoryService.instance.addListener(notifyListeners);
    AppSettings.instance.addListener(notifyListeners);
  }

  void _initTabService() {
    final ts = TabService.instance;
    ts.onSearchTabRequested = (query, displayTitle) => addSearchTab(query, displayTitle);
    ts.onPostTabRequested = (post, context) => addPostTab(post, context);
    ts.onPoolTabRequested = (poolId, poolName) => addPoolTab(poolId, poolName);
    ts.onCloseCurrentTabRequested = () => closeTab(_activeTabIndex);
    ts.onExploreRequested = () => navigateTo(TopNavDestination.explore);
  }

  void navigateTo(TopNavDestination destination) {
    _navDestination = destination;
    notifyListeners();
  }

  void setActiveTabIndex(int index) {
    if (index >= 0 && index < _tabs.length) {
      _activeTabIndex = index;
      _navDestination = TopNavDestination.explore;
      notifyListeners();
    }
  }

  void addSearchTab([String? query, String? displayTitle]) {
    String headerTitle;
    if (displayTitle != null && displayTitle.trim().isNotEmpty) {
      headerTitle = displayTitle.trim();
    } else if (query == null || query.trim().isEmpty) {
      headerTitle = 'Search';
    } else {
      final tokens = query.split(' ').where((t) => t.trim().isNotEmpty).toList();
      final cleanTokens = tokens.where((t) =>
          !t.toLowerCase().startsWith('order:') &&
          !t.toLowerCase().startsWith('rating:') &&
          t.toLowerCase() != '-rating:e' &&
          t.toLowerCase() != '-female' &&
          t.toLowerCase() != '-male').toList();

      headerTitle = cleanTokens.isNotEmpty ? cleanTokens.join(' ') : 'Search';
    }

    if (headerTitle.length > 20) {
      headerTitle = '${headerTitle.substring(0, 18)}…';
    }

    final id = DateTime.now().microsecondsSinceEpoch.toString();
    final tab = AppTabItem(
      id: id,
      title: headerTitle,
      icon: FluentIcons.search,
      content: SearchView(key: ValueKey(id), initialQuery: query),
    );

    _tabs.add(tab);
    _activeTabIndex = _tabs.length - 1;
    _navDestination = TopNavDestination.explore;
    notifyListeners();
  }

  void addPostTab(ImagePost post, [PostNavigationContext? context]) {
    final id = DateTime.now().microsecondsSinceEpoch.toString();
    final tab = AppTabItem(
      id: id,
      title: 'Post #${post.id}',
      icon: FluentIcons.photo2,
      content: PostView(key: ValueKey(id), post: post, navigationContext: context),
    );

    _tabs.add(tab);
    _activeTabIndex = _tabs.length - 1;
    _navDestination = TopNavDestination.explore;
    notifyListeners();
  }

  void addPoolTab(int poolId, [String? poolName]) {
    final title = (poolName != null && poolName.trim().isNotEmpty) ? poolName.trim() : 'Pool #$poolId';
    final id = DateTime.now().microsecondsSinceEpoch.toString();

    final navContext = PostNavigationContext(
      mode: PostNavigationMode.pool,
      poolId: poolId,
      poolName: poolName,
    );

    final tab = AppTabItem(
      id: id,
      title: title.length > 20 ? '${title.substring(0, 18)}…' : title,
      icon: FluentIcons.library,
      content: PostView(
        key: ValueKey(id),
        post: ImagePost(id: poolId, title: title),
        navigationContext: navContext,
      ),
    );

    _tabs.add(tab);
    _activeTabIndex = _tabs.length - 1;
    _navDestination = TopNavDestination.explore;
    notifyListeners();
  }

  void closeTab(int index) {
    if (index >= 0 && index < _tabs.length) {
      _tabs.removeAt(index);
      if (_tabs.isEmpty) {
        addSearchTab();
      } else if (_activeTabIndex >= _tabs.length) {
        _activeTabIndex = _tabs.length - 1;
      }
      notifyListeners();
    }
  }

  Widget getDestinationWidget() {
    switch (_navDestination) {
      case TopNavDestination.pinned:
        return const PinView();
      case TopNavDestination.saved:
        return const SavedSearchesView();
      case TopNavDestination.favourites:
        return const FavouritesView();
      case TopNavDestination.about:
        return const AboutView();
      case TopNavDestination.settings:
        return const SettingsView();
      case TopNavDestination.explore:
        return const SizedBox.shrink();
    }
  }
}
