import 'package:flutter/foundation.dart';
import '../models/image_post.dart';
import '../models/post_navigation_context.dart';

class TabService extends ChangeNotifier {
  static TabService? _instance;
  static TabService get instance => _instance ??= TabService._();

  TabService._();

  void Function(String? query, String? displayTitle)? onSearchTabRequested;
  void Function(ImagePost post, PostNavigationContext? context)? onPostTabRequested;
  void Function(int poolId, String? poolName)? onPoolTabRequested;
  VoidCallback? onCloseCurrentTabRequested;
  VoidCallback? onExploreRequested;

  void openSearchTab([String? query, String? displayTitle]) {
    onSearchTabRequested?.call(query, displayTitle);
  }

  void openPostTab(ImagePost post, [PostNavigationContext? context]) {
    onPostTabRequested?.call(post, context);
  }

  void openPoolTab(int poolId, [String? poolName]) {
    onPoolTabRequested?.call(poolId, poolName);
  }

  void closeCurrentTab() {
    onCloseCurrentTabRequested?.call();
  }

  void openExplore() {
    onExploreRequested?.call();
  }
}
