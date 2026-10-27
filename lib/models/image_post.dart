import 'dart:io';
import '../services/storage_locations.dart';

/// Represents an image post metadata and asset references.
class ImagePost {
  final int id;
  String title;
  String artist;
  String rating; // 'Safe', 'Questionable', 'Explicit'
  int score;
  int favCount;
  int width;
  int height;
  String fileExtension;
  String fileSizeFormatted;
  DateTime createdAt;

  String previewUrl;
  String sampleUrl;
  String fullUrl;

  List<String> generalTags;
  List<String> artistTags;
  List<String> characterTags;
  List<String> speciesTags;
  List<String> copyrightTags;

  List<String> sources;
  String description;

  int? parentId;
  bool hasChildren;
  List<int> childIds;
  List<int> poolIds;

  String? localSamplePath;
  bool isBookmarked;
  bool isFavorited;
  int? ourScore;

  ImagePost({
    required this.id,
    this.title = '',
    this.artist = 'Unknown',
    this.rating = 'Safe',
    this.score = 0,
    this.favCount = 0,
    this.width = 0,
    this.height = 0,
    this.fileExtension = 'jpg',
    this.fileSizeFormatted = '',
    DateTime? createdAt,
    this.previewUrl = '',
    this.sampleUrl = '',
    this.fullUrl = '',
    List<String>? generalTags,
    List<String>? artistTags,
    List<String>? characterTags,
    List<String>? speciesTags,
    List<String>? copyrightTags,
    List<String>? sources,
    this.description = '',
    this.parentId,
    this.hasChildren = false,
    List<int>? childIds,
    List<int>? poolIds,
    this.localSamplePath,
    this.isBookmarked = false,
    this.isFavorited = false,
    this.ourScore,
  })  : createdAt = createdAt ?? DateTime.now().toUtc(),
        generalTags = generalTags ?? [],
        artistTags = artistTags ?? [],
        characterTags = characterTags ?? [],
        speciesTags = speciesTags ?? [],
        copyrightTags = copyrightTags ?? [],
        sources = sources ?? [],
        childIds = childIds ?? [],
        poolIds = poolIds ?? [];

  bool get isVideo {
    final ext = fileExtension.trim().toLowerCase();
    return ext == 'webm' || ext == 'mp4' || ext == 'mkv' || ext == 'avi' || ext == 'mov' || ext == 'flv';
  }

  bool get isAnimatedGif {
    return fileExtension.trim().toLowerCase() == 'gif';
  }

  String get mediaTypeLabel => isVideo ? 'Video' : (isAnimatedGif ? 'GIF' : 'Image');

  bool get hasParent => parentId != null && parentId! > 0;
  bool get hasRelationships => hasParent || hasChildren;
  bool get isInPool => poolIds.isNotEmpty;

  String get dimensionText => '$width × $height';
  String get displayName => title.trim().isEmpty ? 'Post #$id ($artist)' : title;
  String get scoreText => 'Score: $score';
  String get favouritesText => 'Favourites: $favCount';

  String? get effectiveSamplePath {
    var path = localSamplePath;
    if (path == null || path.isEmpty || !File(path).existsSync()) {
      path = StorageLocations.getExistingCachedImagePath(id);
      if (path != null) {
        localSamplePath = path;
      }
    }
    if (path != null && File(path).existsSync()) {
      return path;
    }
    return null;
  }

  String get effectiveSampleUrl {
    final local = effectiveSamplePath;
    if (local != null) return local;
    if (sampleUrl.isNotEmpty) return sampleUrl;
    if (previewUrl.isNotEmpty) return previewUrl;
    return fullUrl;
  }

  String get effectivePreviewUrl {
    final local = effectiveSamplePath;
    if (local != null) return local;
    return previewUrl.isNotEmpty ? previewUrl : fullUrl;
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'title': title,
      'artist': artist,
      'rating': rating,
      'score': score,
      'fav_count': favCount,
      'width': width,
      'height': height,
      'file_extension': fileExtension,
      'file_size_formatted': fileSizeFormatted,
      'created_at': createdAt.toIso8601String(),
      'preview_url': previewUrl,
      'sample_url': sampleUrl,
      'full_url': fullUrl,
      'general_tags': generalTags,
      'artist_tags': artistTags,
      'character_tags': characterTags,
      'species_tags': speciesTags,
      'copyright_tags': copyrightTags,
      'sources': sources,
      'description': description,
      'parent_id': parentId,
      'has_children': hasChildren,
      'child_ids': childIds,
      'pool_ids': poolIds,
      'local_sample_path': localSamplePath,
      'is_bookmarked': isBookmarked,
      'is_favorited': isFavorited,
      'our_score': ourScore,
    };
  }

  factory ImagePost.fromJson(Map<String, dynamic> json) {
    DateTime parseDate(dynamic d) {
      if (d == null) return DateTime.now().toUtc();
      try {
        return DateTime.parse(d.toString());
      } catch (_) {
        return DateTime.now().toUtc();
      }
    }

    List<String> parseStringList(dynamic list) {
      if (list is List) {
        return list.map((e) => e.toString()).toList();
      }
      return [];
    }

    List<int> parseIntList(dynamic list) {
      if (list is List) {
        return list.map((e) => (e as num).toInt()).toList();
      }
      return [];
    }

    return ImagePost(
      id: (json['id'] as num?)?.toInt() ?? 0,
      title: json['title']?.toString() ?? '',
      artist: json['artist']?.toString() ?? 'Unknown',
      rating: json['rating']?.toString() ?? 'Safe',
      score: (json['score'] as num?)?.toInt() ?? 0,
      favCount: (json['fav_count'] as num?)?.toInt() ?? (json['favCount'] as num?)?.toInt() ?? 0,
      width: (json['width'] as num?)?.toInt() ?? 0,
      height: (json['height'] as num?)?.toInt() ?? 0,
      fileExtension: json['file_extension']?.toString() ?? json['fileExtension']?.toString() ?? 'jpg',
      fileSizeFormatted: json['file_size_formatted']?.toString() ?? json['fileSizeFormatted']?.toString() ?? '',
      createdAt: parseDate(json['created_at'] ?? json['createdAt']),
      previewUrl: json['preview_url']?.toString() ?? json['previewUrl']?.toString() ?? '',
      sampleUrl: json['sample_url']?.toString() ?? json['sampleUrl']?.toString() ?? '',
      fullUrl: json['full_url']?.toString() ?? json['fullUrl']?.toString() ?? '',
      generalTags: parseStringList(json['general_tags'] ?? json['generalTags']),
      artistTags: parseStringList(json['artist_tags'] ?? json['artistTags']),
      characterTags: parseStringList(json['character_tags'] ?? json['characterTags']),
      speciesTags: parseStringList(json['species_tags'] ?? json['speciesTags']),
      copyrightTags: parseStringList(json['copyright_tags'] ?? json['copyrightTags']),
      sources: parseStringList(json['sources']),
      description: json['description']?.toString() ?? '',
      parentId: (json['parent_id'] as num?)?.toInt() ?? (json['parentId'] as num?)?.toInt(),
      hasChildren: json['has_children'] == true || json['hasChildren'] == true,
      childIds: parseIntList(json['child_ids'] ?? json['childIds']),
      poolIds: parseIntList(json['pool_ids'] ?? json['poolIds']),
      localSamplePath: json['local_sample_path']?.toString() ?? json['localSamplePath']?.toString(),
      isBookmarked: json['is_bookmarked'] == true || json['isBookmarked'] == true,
      isFavorited: json['is_favorited'] == true || json['isFavorited'] == true,
      ourScore: (json['our_score'] as num?)?.toInt() ?? (json['ourScore'] as num?)?.toInt(),
    );
  }
}
