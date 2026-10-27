import 'package:fluent_ui/fluent_ui.dart';
import 'package:provider/provider.dart';
import '../services/app_settings.dart';
import '../services/search_history_service.dart';
import '../viewmodels/main_viewmodel.dart';
import '../widgets/app_title_bar.dart';
import 'about_view.dart';
import 'favourites_view.dart';
import 'pin_view.dart';
import 'saved_searches_view.dart';
import 'settings_view.dart';

class MainView extends StatefulWidget {
  const MainView({super.key});

  @override
  State<MainView> createState() => _MainViewState();
}

class _MainViewState extends State<MainView> {
  int _selectedNavIndex = 0;
  bool _isPaneOpen = true;

  @override
  Widget build(BuildContext context) {
    final vm = Provider.of<MainViewModel>(context);
    final settings = AppSettings.instance;

    // Synchronize top nav selection with ViewModel destination
    switch (vm.navDestination) {
      case TopNavDestination.explore:
        _selectedNavIndex = 0;
        break;
      case TopNavDestination.pinned:
        _selectedNavIndex = 1;
        break;
      case TopNavDestination.saved:
        _selectedNavIndex = 2;
        break;
      case TopNavDestination.favourites:
        _selectedNavIndex = 3;
        break;
      case TopNavDestination.about:
        _selectedNavIndex = 4;
        break;
      case TopNavDestination.settings:
        _selectedNavIndex = 5;
        break;
    }

    return Column(
      children: [
        // App Title Bar
        AppTitleBar(
          onTogglePane: () {
            setState(() => _isPaneOpen = !_isPaneOpen);
          },
        ),

        // Main Navigation View
        Expanded(
          child: NavigationView(
            pane: NavigationPane(
              selected: _selectedNavIndex,
              displayMode: _isPaneOpen ? PaneDisplayMode.expanded : PaneDisplayMode.compact,
              onChanged: (index) {
                setState(() => _selectedNavIndex = index);
                switch (index) {
                  case 0:
                    vm.navigateTo(TopNavDestination.explore);
                    break;
                  case 1:
                    vm.navigateTo(TopNavDestination.pinned);
                    break;
                  case 2:
                    vm.navigateTo(TopNavDestination.saved);
                    break;
                  case 3:
                    vm.navigateTo(TopNavDestination.favourites);
                    break;
                  case 4:
                    vm.navigateTo(TopNavDestination.about);
                    break;
                  case 5:
                    vm.navigateTo(TopNavDestination.settings);
                    break;
                }
              },
              items: [
                PaneItem(
                  icon: const Icon(FluentIcons.search),
                  title: const Text('Explore'),
                  body: _buildTabView(context, vm),
                ),
                PaneItem(
                  icon: const Icon(FluentIcons.pin),
                  title: const Text('Pinned'),
                  infoBadge: vm.pinnedCount > 0 ? InfoBadge(source: Text('${vm.pinnedCount}')) : null,
                  body: const PinView(),
                ),
                PaneItem(
                  icon: const Icon(FluentIcons.save),
                  title: const Text('Saved Searches'),
                  infoBadge: vm.savedCount > 0 ? InfoBadge(source: Text('${vm.savedCount}')) : null,
                  body: const SavedSearchesView(),
                ),
                PaneItem(
                  icon: const Icon(FluentIcons.heart),
                  title: const Text('Favourites'),
                  infoBadge: (vm.favouritesCount != null && vm.favouritesCount! > 0)
                      ? InfoBadge(source: Text('${vm.favouritesCount}'))
                      : null,
                  body: const FavouritesView(),
                ),
                PaneItem(
                  icon: const Icon(FluentIcons.info),
                  title: const Text('About'),
                  body: const AboutView(),
                ),
              ],
              footerItems: [
                PaneItem(
                  icon: const Icon(FluentIcons.settings),
                  title: const Text('Settings'),
                  body: const SettingsView(),
                ),
                PaneItemWidgetAdapter(
                  child: _isPaneOpen
                      ? _buildExpandedSidebarFooter(context, vm, settings)
                      : _buildCollapsedSidebarFooter(context),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildTabView(BuildContext context, MainViewModel vm) {
    return TabView(
      currentIndex: vm.activeTabIndex,
      onChanged: (index) => vm.setActiveTabIndex(index),
      onNewPressed: () => vm.addSearchTab(),
      onReorder: (oldIndex, newIndex) {
        // Tab reordering if needed
      },
      tabs: List.generate(vm.tabs.length, (index) {
        final tab = vm.tabs[index];
        return Tab(
          text: Text(tab.title),
          icon: Icon(tab.icon, size: 14),
          body: tab.content,
          onClosed: () => vm.closeTab(index),
        );
      }),
    );
  }

  Widget _buildExpandedSidebarFooter(BuildContext context, MainViewModel vm, AppSettings settings) {
    final theme = FluentTheme.of(context);
    final history = SearchHistoryService.instance.history;

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          // Previous Searches Header
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(FluentIcons.history, size: 13, color: theme.resources.textFillColorSecondary),
                  const SizedBox(width: 6),
                  Text(
                    'Previous searches',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: theme.resources.textFillColorSecondary,
                    ),
                  ),
                ],
              ),
              if (history.isNotEmpty)
                Tooltip(
                  message: 'Clear history',
                  child: IconButton(
                    icon: Icon(FluentIcons.delete, size: 11, color: theme.resources.textFillColorTertiary),
                    onPressed: () => SearchHistoryService.instance.clearHistory(),
                  ),
                ),
            ],
          ),

          const SizedBox(height: 4),

          // Resize handle (drag handle)
          GestureDetector(
            behavior: HitTestBehavior.opaque,
            onVerticalDragUpdate: (details) {
              // Dragging upwards increases height
              vm.searchHistoryHeight = vm.searchHistoryHeight - details.delta.dy;
            },
            child: Container(
              height: 12,
              alignment: Alignment.center,
              child: Container(
                width: 48,
                height: 4,
                decoration: BoxDecoration(
                  color: theme.resources.textFillColorSecondary.withValues(alpha: 0.5),
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
          ),

          const SizedBox(height: 4),

          // Scrollable previous searches container
          Container(
            height: vm.searchHistoryHeight,
            decoration: BoxDecoration(
              color: theme.cardColor,
              borderRadius: BorderRadius.circular(6),
              border: Border.all(color: theme.resources.cardStrokeColorDefault),
            ),
            child: history.isEmpty
                ? Center(
                    child: Text(
                      'No recent searches',
                      style: TextStyle(fontSize: 12, color: theme.resources.textFillColorTertiary),
                    ),
                  )
                : ListView.builder(
                    primary: false,
                    itemCount: history.length,
                    itemBuilder: (context, index) {
                      final query = history[index];
                      return Container(
                        height: 32,
                        padding: const EdgeInsets.symmetric(horizontal: 8),
                        child: Row(
                          children: [
                            Expanded(
                              child: GestureDetector(
                                behavior: HitTestBehavior.opaque,
                                onTap: () => vm.addSearchTab(query),
                                child: Text(
                                  query,
                                  style: const TextStyle(fontSize: 12),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                            ),
                            IconButton(
                              icon: Icon(FluentIcons.chrome_close, size: 9, color: theme.resources.textFillColorTertiary),
                              onPressed: () => SearchHistoryService.instance.removeSearch(query),
                            ),
                          ],
                        ),
                      );
                    },
                  ),
          ),

          const SizedBox(height: 12),

          // Rating filter
          Row(
            children: [
              Icon(FluentIcons.filter, size: 13, color: theme.resources.textFillColorSecondary),
              const SizedBox(width: 6),
              Text(
                'Rating',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: theme.resources.textFillColorSecondary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          ComboBox<String>(
            value: settings.ratingFilter,
            isExpanded: true,
            items: const [
              ComboBoxItem(value: 'Safe', child: Text('Safe')),
              ComboBoxItem(value: 'Questionable', child: Text('Questionable')),
              ComboBoxItem(value: 'Explicit', child: Text('Explicit')),
            ],
            onChanged: (val) {
              if (val != null) {
                settings.ratingFilter = val;
              }
            },
          ),

          const SizedBox(height: 12),

          // Show gender filter
          Row(
            children: [
              Icon(FluentIcons.people, size: 13, color: theme.resources.textFillColorSecondary),
              const SizedBox(width: 6),
              Text(
                'Show gender',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: theme.resources.textFillColorSecondary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          ComboBox<String>(
            value: settings.showGender,
            isExpanded: true,
            items: const [
              ComboBoxItem(value: 'All', child: Text('All')),
              ComboBoxItem(value: 'Male', child: Text('Male')),
              ComboBoxItem(value: 'Female', child: Text('Female')),
            ],
            onChanged: (val) {
              if (val != null) {
                settings.showGender = val;
              }
            },
          ),
          const SizedBox(height: 12),
        ],
      ),
    );
  }

  Widget _buildCollapsedSidebarFooter(BuildContext context) {
    final theme = FluentTheme.of(context);

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Tooltip(
            message: 'Previous searches',
            child: Icon(FluentIcons.history, size: 16, color: theme.resources.textFillColorSecondary),
          ),
          const SizedBox(height: 12),
          Tooltip(
            message: 'Rating filter',
            child: Icon(FluentIcons.filter, size: 16, color: theme.resources.textFillColorSecondary),
          ),
          const SizedBox(height: 12),
          Tooltip(
            message: 'Show gender',
            child: Icon(FluentIcons.people, size: 16, color: theme.resources.textFillColorSecondary),
          ),
        ],
      ),
    );
  }
}
