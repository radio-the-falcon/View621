import 'image_post.dart';

enum PostNavigationMode {
  searchResults,
  pool,
  singlePost,
}

class PostNavigationContext {
  PostNavigationMode mode;
  List<ImagePost> posts;
  int currentIndex;
  int? poolId;
  String? poolName;
  List<int>? poolPostIds;
  String? searchQuery;

  PostNavigationContext({
    this.mode = PostNavigationMode.searchResults,
    List<ImagePost>? posts,
    this.currentIndex = 0,
    this.poolId,
    this.poolName,
    this.poolPostIds,
    this.searchQuery,
  }) : posts = posts ?? [];

  ImagePost? get currentPost {
    if (currentIndex >= 0 && currentIndex < posts.length) {
      return posts[currentIndex];
    }
    return null;
  }
}
