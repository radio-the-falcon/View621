import 'dart:io';
import 'package:http/http.dart' as http;
import 'package:path/path.dart' as p;
import '../models/image_post.dart';
import 'storage_locations.dart';

/// Downloads and caches sample media in ~/.View621/cache/images for offline access.
class ImageCacheService {
  static ImageCacheService? _instance;
  static ImageCacheService get instance => _instance ??= ImageCacheService._();

  static final http.Client _client = http.Client();

  ImageCacheService._() {
    StorageLocations.ensureDirectories();
  }

  String? getCachedImagePath(int postId) {
    return StorageLocations.getExistingCachedImagePath(postId);
  }

  Future<String?> cacheSampleImage(ImagePost post) async {
    if (post.id <= 0) return null;

    final existing = StorageLocations.getExistingCachedImagePath(post.id);
    if (existing != null) {
      post.localSamplePath = existing;
      return existing;
    }

    final downloadUrl = post.sampleUrl.isNotEmpty
        ? post.sampleUrl
        : (post.previewUrl.isNotEmpty ? post.previewUrl : post.fullUrl);

    if (downloadUrl.isEmpty || !downloadUrl.startsWith('http')) {
      return null;
    }

    try {
      StorageLocations.ensureDirectories();
      var ext = post.fileExtension.replaceAll('.', '').trim();
      if (ext.isEmpty) ext = 'jpg';

      final destinationPath = p.join(StorageLocations.imageCacheDirectory, '${post.id}.$ext');
      final uri = Uri.parse(downloadUrl);

      final request = http.Request('GET', uri);
      request.headers['User-Agent'] = 'View621/1.0 (by View621_user on e621)';
      final response = await _client.send(request);

      if (response.statusCode == 200) {
        final file = File(destinationPath);
        final sink = file.openWrite();
        await response.stream.pipe(sink);
        await sink.flush();
        await sink.close();

        post.localSamplePath = destinationPath;
        return destinationPath;
      }
    } catch (e) {
      StorageLocations.log('[ImageCacheService Download Error] Post #${post.id}: $e');
    }

    return null;
  }

  void deleteCachedImage(int postId) {
    if (postId <= 0) return;
    try {
      final existing = StorageLocations.getExistingCachedImagePath(postId);
      if (existing != null) {
        final file = File(existing);
        if (file.existsSync()) {
          file.deleteSync();
        }
      }
    } catch (e) {
      StorageLocations.log('[ImageCacheService Delete Error] Post #$postId: $e');
    }
  }

  void clearAllCachedImages() {
    try {
      final dir = Directory(StorageLocations.imageCacheDirectory);
      if (dir.existsSync()) {
        for (final entity in dir.listSync()) {
          if (entity is File) {
            entity.deleteSync();
          }
        }
      }
    } catch (e) {
      StorageLocations.log('[ImageCacheService Clear Error] $e');
    }
  }
}
