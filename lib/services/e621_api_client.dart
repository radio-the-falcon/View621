import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/e621_dto.dart';
import '../models/image_post.dart';
import 'app_settings.dart';
import 'bookmark_service.dart';
import 'storage_locations.dart';

/// Production API client for e621 / e926 following the OpenAPI specification.
class E621ApiClient {
  static final http.Client _client = http.Client();

  Map<String, String> _buildHeaders() {
    final headers = <String, String>{
      'User-Agent': 'View621/1.0 (by View621_user on e621)',
      'Accept': 'application/json',
    };

    final settings = AppSettings.instance;
    if (settings.isLoggedIn) {
      final token = base64Encode(utf8.encode('${settings.username}:${settings.apiKey}'));
      headers['Authorization'] = 'Basic $token';
    }

    return headers;
  }

  Future<List<ImagePost>> searchPosts(String query, {int page = 1, int limit = 40}) async {
    final effectiveLimit = limit.clamp(1, 320);
    final effectivePage = page.clamp(1, 750);
    final baseUrl = AppSettings.instance.apiBaseUrl;

    var url = '$baseUrl/posts.json?limit=$effectiveLimit&page=$effectivePage';
    if (query.trim().isNotEmpty) {
      url += '&tags=${Uri.encodeComponent(query.trim())}';
    }

    try {
      final response = await _client.get(Uri.parse(url), headers: _buildHeaders());
      if (response.statusCode != 200) {
        return [];
      }

      final json = jsonDecode(response.body);
      if (json is Map<String, dynamic>) {
        final parsed = E621PostsResponse.fromJson(json);
        final posts = parsed.posts.map(mapToImagePost).toList();
        for (final p in posts) {
          p.isBookmarked = BookmarkService.instance.isBookmarked(p.id);
        }
        return posts;
      }
    } catch (e) {
      StorageLocations.log('[E621ApiClient] Search error: $e');
    }
    return [];
  }

  Future<ImagePost?> getPostById(int id) async {
    if (id <= 0) return null;

    final cached = BookmarkService.instance.getCachedPost(id);
    if (cached != null) {
      cached.isBookmarked = BookmarkService.instance.isBookmarked(id);
      return cached;
    }

    final baseUrl = AppSettings.instance.apiBaseUrl;
    final url = '$baseUrl/posts/$id.json';

    try {
      final response = await _client.get(Uri.parse(url), headers: _buildHeaders());
      if (response.statusCode == 404) return null;
      if (response.statusCode != 200) return null;

      final json = jsonDecode(response.body);
      if (json is Map<String, dynamic>) {
        final parsed = E621SinglePostResponse.fromJson(json);
        if (parsed.post != null) {
          final mapped = mapToImagePost(parsed.post!);
          mapped.isBookmarked = BookmarkService.instance.isBookmarked(id);
          BookmarkService.instance.cachePost(mapped);
          return mapped;
        }
      }
    } catch (e) {
      StorageLocations.log('[E621ApiClient] GetPost error: $e');
    }
    return null;
  }

  Future<E621PoolDto?> getPoolById(int poolId) async {
    if (poolId <= 0) return null;
    final baseUrl = AppSettings.instance.apiBaseUrl;
    final url = '$baseUrl/pools/$poolId.json';

    try {
      final response = await _client.get(Uri.parse(url), headers: _buildHeaders());
      if (response.statusCode != 200) return null;

      final json = jsonDecode(response.body);
      if (json is Map<String, dynamic>) {
        return E621PoolDto.fromJson(json);
      }
    } catch (e) {
      StorageLocations.log('[E621ApiClient] GetPool error: $e');
    }
    return null;
  }

  Future<List<String>> autocompleteTags(String query) async {
    if (query.trim().length < 2) return [];
    final baseUrl = AppSettings.instance.apiBaseUrl;
    final url = '$baseUrl/tags/autocomplete.json?search[name_matches]=${Uri.encodeComponent(query.trim())}';

    try {
      final response = await _client.get(Uri.parse(url), headers: _buildHeaders());
      if (response.statusCode != 200) return [];

      final list = jsonDecode(response.body);
      if (list is List) {
        final parsed = list.map((e) => E621TagAutocompleteDto.fromJson(e as Map<String, dynamic>)).toList();
        parsed.sort((a, b) => b.postCount.compareTo(a.postCount));
        return parsed.map((t) => t.name).where((n) => n.trim().isNotEmpty).take(12).toList();
      }
    } catch (_) {}
    return [];
  }

  Future<int?> getPostCount(String query) async {
    try {
      final baseUrl = AppSettings.instance.apiBaseUrl;
      final trimmed = query.trim();

      // Strategy 1: Check single tag metadata via /tags.json
      if (trimmed.isNotEmpty && !trimmed.contains(' ')) {
        final tagUrl = '$baseUrl/tags.json?search[name]=${Uri.encodeComponent(trimmed)}';
        final tagResp = await _client.get(Uri.parse(tagUrl), headers: _buildHeaders());
        if (tagResp.statusCode == 200) {
          final data = jsonDecode(tagResp.body);
          if (data is List && data.isNotEmpty) {
            final first = data[0];
            if (first is Map && first.containsKey('post_count')) {
              return (first['post_count'] as num).toInt();
            }
          }
        }
      }

      // Strategy 2: Scrape approximate/total count from provider's /posts HTML
      var webUrl = '$baseUrl/posts';
      if (trimmed.isNotEmpty) {
        webUrl += '?tags=${Uri.encodeComponent(trimmed)}';
      }
      final webResp = await _client.get(Uri.parse(webUrl), headers: _buildHeaders());
      if (webResp.statusCode == 200) {
        final html = webResp.body;
        final match = RegExp(r'posts-info-count[^>]*>.*?data-count="(\d+)"', caseSensitive: false, dotAll: true)
                .firstMatch(html) ??
            RegExp(r'approximate-count[^>]*data-count="(\d+)"', caseSensitive: false, dotAll: true)
                .firstMatch(html);
        if (match != null) {
          final val = int.tryParse(match.group(1) ?? '');
          if (val != null) return val;
        }
      }
    } catch (e) {
      StorageLocations.log('[E621ApiClient] GetPostCount error for query "$query": $e');
    }
    return null;
  }

  Future<({bool success, String message})> verifyLogin(String username, String apiKey) async {
    final cleanUser = username.trim();
    final cleanKey = apiKey.trim();

    if (cleanUser.isEmpty || cleanKey.isEmpty) {
      return (success: false, message: 'Username and API key must not be empty.');
    }

    try {
      final baseUrl = AppSettings.instance.apiBaseUrl;
      final url = '$baseUrl/favorites.json?limit=1';
      final token = base64Encode(utf8.encode('$cleanUser:$cleanKey'));

      final headers = {
        'User-Agent': 'View621/1.0 (by View621_user on e621)',
        'Accept': 'application/json',
        'Authorization': 'Basic $token',
      };

      final response = await _client.get(Uri.parse(url), headers: headers);
      if (response.statusCode == 200) {
        return (success: true, message: 'Logged in successfully as $cleanUser.');
      }
      if (response.statusCode == 401) {
        return (success: false, message: 'Invalid username or API key.');
      }
      if (response.statusCode == 403) {
        return (success: false, message: 'Access forbidden. Ensure API access is enabled on your e621 account.');
      }
      return (success: false, message: 'Verification failed (HTTP ${response.statusCode} ${response.reasonPhrase}).');
    } catch (e) {
      return (success: false, message: 'Connection error: $e');
    }
  }

  Future<List<ImagePost>> getFavorites({int page = 1, int limit = 40}) async {
    final settings = AppSettings.instance;
    if (!settings.isLoggedIn || settings.username.isEmpty) {
      return [];
    }

    final effectiveLimit = limit.clamp(1, 320);
    final effectivePage = page.clamp(1, 750);
    final baseUrl = settings.apiBaseUrl;

    // Strategy 1: Attempt direct /favorites.json
    try {
      final url = '$baseUrl/favorites.json?limit=$effectiveLimit&page=$effectivePage';
      final response = await _client.get(Uri.parse(url), headers: _buildHeaders());
      if (response.statusCode == 200) {
        final json = jsonDecode(response.body);
        if (json is Map<String, dynamic>) {
          final parsed = E621PostsResponse.fromJson(json);
          if (parsed.posts.isNotEmpty) {
            final posts = parsed.posts.map(mapToImagePost).toList();
            for (final p in posts) {
              p.isBookmarked = BookmarkService.instance.isBookmarked(p.id);
            }
            return posts;
          }
        }
      }
    } catch (e) {
      StorageLocations.log('[E621ApiClient] /favorites.json query failed: $e');
    }

    // Strategy 2: Standard e621 tag search fav:{username}
    return await searchPosts('fav:${settings.username}', page: effectivePage, limit: effectiveLimit);
  }

  Future<int?> getFavoritesCount() async {
    final settings = AppSettings.instance;
    if (!settings.isLoggedIn) return null;
    final baseUrl = settings.apiBaseUrl;

    // Strategy 1: /users/me.json
    try {
      final meUrl = '$baseUrl/users/me.json';
      final response = await _client.get(Uri.parse(meUrl), headers: _buildHeaders());
      if (response.statusCode == 200) {
        final doc = jsonDecode(response.body);
        if (doc is Map && doc.containsKey('favorite_count')) {
          final count = (doc['favorite_count'] as num).toInt();
          return count;
        }
      }
    } catch (e) {
      StorageLocations.log('[E621ApiClient] /users/me.json count query failed: $e');
    }

    // Strategy 2: /users.json?search[name_matches]={username}
    try {
      final userUrl = '$baseUrl/users.json?search[name_matches]=${Uri.encodeComponent(settings.username.trim())}';
      final response = await _client.get(Uri.parse(userUrl), headers: _buildHeaders());
      if (response.statusCode == 200) {
        final doc = jsonDecode(response.body);
        dynamic userElement;
        if (doc is List && doc.isNotEmpty) {
          userElement = doc[0];
        } else if (doc is Map && doc['users'] is List && (doc['users'] as List).isNotEmpty) {
          userElement = doc['users'][0];
        }

        if (userElement is Map && userElement.containsKey('favorite_count')) {
          return (userElement['favorite_count'] as num).toInt();
        }
      }
    } catch (e) {
      StorageLocations.log('[E621ApiClient] /users.json count query failed: $e');
    }

    // Strategy 3: Check /favorites.json or fav:{username}
    try {
      final favs = await getFavorites(page: 1, limit: 320);
      return favs.length;
    } catch (_) {}

    return null;
  }

  Future<bool> addFavorite(int postId) async {
    if (postId <= 0 || !AppSettings.instance.isLoggedIn) return false;
    try {
      final baseUrl = AppSettings.instance.apiBaseUrl;
      final url = '$baseUrl/favorites.json';
      final response = await _client.post(
        Uri.parse(url),
        headers: _buildHeaders(),
        body: {'post_id': postId.toString()},
      );
      if (response.statusCode == 200 || response.statusCode == 201 || response.statusCode == 422) {
        return true;
      }
      return false;
    } catch (_) {
      return false;
    }
  }

  Future<bool> removeFavorite(int postId) async {
    if (postId <= 0 || !AppSettings.instance.isLoggedIn) return false;
    try {
      final baseUrl = AppSettings.instance.apiBaseUrl;
      final url = '$baseUrl/favorites/$postId.json';
      final response = await _client.delete(Uri.parse(url), headers: _buildHeaders());
      return response.statusCode == 200 || response.statusCode == 204 || response.statusCode == 404;
    } catch (_) {
      return false;
    }
  }

  Future<({bool success, int newScore, int? ourScore})> votePost(int postId, int score) async {
    if (postId <= 0 || !AppSettings.instance.isLoggedIn) {
      return (success: false, newScore: 0, ourScore: null);
    }

    try {
      final baseUrl = AppSettings.instance.apiBaseUrl;
      final url = '$baseUrl/posts/$postId/votes.json';
      final response = await _client.post(
        Uri.parse(url),
        headers: _buildHeaders(),
        body: {
          'score': score.toString(),
          'no_unvote': 'false',
        },
      );

      if (response.statusCode != 200) {
        return (success: false, newScore: 0, ourScore: null);
      }

      final json = jsonDecode(response.body);
      if (json is Map<String, dynamic>) {
        final parsed = E621VoteResponseDto.fromJson(json);
        final total = parsed.score?.total ?? 0;
        final our = parsed.ourScore;
        return (success: true, newScore: total, ourScore: our);
      }
    } catch (e) {
      StorageLocations.log('[E621ApiClient] Vote error: $e');
    }
    return (success: false, newScore: 0, ourScore: null);
  }

  static ImagePost mapToImagePost(E621PostDto dto) {
    final artist = (dto.tags?.artist != null && dto.tags!.artist.isNotEmpty)
        ? dto.tags!.artist.join(', ')
        : 'Unknown';

    String rating;
    switch (dto.rating.toLowerCase()) {
      case 's':
        rating = 'Safe';
        break;
      case 'q':
        rating = 'Questionable';
        break;
      case 'e':
        rating = 'Explicit';
        break;
      default:
        rating = dto.rating;
        break;
    }

    final ext = dto.file?.ext.toLowerCase() ?? 'jpg';
    final isImage = ext == 'jpg' || ext == 'jpeg' || ext == 'png' || ext == 'webp' || ext == 'gif';

    var previewUrl = dto.preview?.url ?? '';
    if (previewUrl.isEmpty && isImage) {
      previewUrl = dto.sample?.url ?? dto.file?.url ?? '';
    }

    String sampleUrl = '';
    if (dto.sample != null && dto.sample!.has && (dto.sample!.url?.isNotEmpty ?? false)) {
      sampleUrl = dto.sample!.url!;
    } else if (isImage && (dto.file?.url?.isNotEmpty ?? false)) {
      sampleUrl = dto.file!.url!;
    } else {
      sampleUrl = previewUrl;
    }

    final fullUrl = (dto.file?.url?.isNotEmpty ?? false) ? dto.file!.url! : sampleUrl;
    final childIds = dto.relationships?.children != null ? List<int>.from(dto.relationships!.children) : <int>[];
    final poolIds = List<int>.from(dto.pools);

    return ImagePost(
      id: dto.id,
      title: 'Post #${dto.id}',
      artist: artist,
      rating: rating,
      score: dto.score?.total ?? 0,
      favCount: dto.favCount,
      isFavorited: dto.isFavorited,
      width: dto.file?.width ?? dto.sample?.width ?? 0,
      height: dto.file?.height ?? dto.sample?.height ?? 0,
      fileExtension: dto.file?.ext ?? 'jpg',
      fileSizeFormatted: _formatBytes(dto.file?.size ?? 0),
      createdAt: dto.createdAt,
      previewUrl: previewUrl,
      sampleUrl: sampleUrl,
      fullUrl: fullUrl,
      generalTags: dto.tags?.general != null ? List<String>.from(dto.tags!.general) : [],
      artistTags: dto.tags?.artist != null ? List<String>.from(dto.tags!.artist) : [],
      characterTags: dto.tags?.character != null ? List<String>.from(dto.tags!.character) : [],
      speciesTags: dto.tags?.species != null ? List<String>.from(dto.tags!.species) : [],
      copyrightTags: dto.tags?.copyright != null ? List<String>.from(dto.tags!.copyright) : [],
      sources: List<String>.from(dto.sources),
      description: dto.description,
      parentId: dto.relationships?.parentId,
      hasChildren: dto.relationships?.hasChildren ?? (childIds.isNotEmpty),
      childIds: childIds,
      poolIds: poolIds,
    );
  }

  static String _formatBytes(int bytes) {
    if (bytes <= 0) return '0 KB';
    const units = ['B', 'KB', 'MB', 'GB'];
    var order = 0;
    double size = bytes.toDouble();
    while (size >= 1024 && order < units.length - 1) {
      order++;
      size /= 1024;
    }
    return '${size.toStringAsFixed(1)} ${units[order]}';
  }
}
