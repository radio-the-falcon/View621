class E621PostsResponse {
  final List<E621PostDto> posts;

  E621PostsResponse({required this.posts});

  factory E621PostsResponse.fromJson(Map<String, dynamic> json) {
    final list = json['posts'] as List<dynamic>? ?? [];
    return E621PostsResponse(
      posts: list.map((e) => E621PostDto.fromJson(e as Map<String, dynamic>)).toList(),
    );
  }
}

class E621SinglePostResponse {
  final E621PostDto? post;

  E621SinglePostResponse({this.post});

  factory E621SinglePostResponse.fromJson(Map<String, dynamic> json) {
    final p = json['post'];
    return E621SinglePostResponse(
      post: p != null ? E621PostDto.fromJson(p as Map<String, dynamic>) : null,
    );
  }
}

class E621PostDto {
  final int id;
  final DateTime createdAt;
  final E621FileDto? file;
  final E621PreviewDto? preview;
  final E621SampleDto? sample;
  final E621ScoreDto? score;
  final E621TagsDto? tags;
  final String rating;
  final int favCount;
  final List<String> sources;
  final String description;
  final E621RelationshipsDto? relationships;
  final List<int> pools;
  final bool isFavorited;

  E621PostDto({
    required this.id,
    required this.createdAt,
    this.file,
    this.preview,
    this.sample,
    this.score,
    this.tags,
    required this.rating,
    required this.favCount,
    required this.sources,
    required this.description,
    this.relationships,
    required this.pools,
    required this.isFavorited,
  });

  factory E621PostDto.fromJson(Map<String, dynamic> json) {
    DateTime parseDate(dynamic d) {
      if (d == null) return DateTime.now().toUtc();
      try {
        return DateTime.parse(d.toString());
      } catch (_) {
        return DateTime.now().toUtc();
      }
    }

    final sourcesList = (json['sources'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [];
    final poolsList = (json['pools'] as List<dynamic>?)?.map((e) => (e as num).toInt()).toList() ?? [];

    return E621PostDto(
      id: (json['id'] as num?)?.toInt() ?? 0,
      createdAt: parseDate(json['created_at']),
      file: json['file'] != null ? E621FileDto.fromJson(json['file'] as Map<String, dynamic>) : null,
      preview: json['preview'] != null ? E621PreviewDto.fromJson(json['preview'] as Map<String, dynamic>) : null,
      sample: json['sample'] != null ? E621SampleDto.fromJson(json['sample'] as Map<String, dynamic>) : null,
      score: json['score'] != null ? E621ScoreDto.fromJson(json['score'] as Map<String, dynamic>) : null,
      tags: json['tags'] != null ? E621TagsDto.fromJson(json['tags'] as Map<String, dynamic>) : null,
      rating: json['rating']?.toString() ?? 's',
      favCount: (json['fav_count'] as num?)?.toInt() ?? 0,
      sources: sourcesList,
      description: json['description']?.toString() ?? '',
      relationships: json['relationships'] != null
          ? E621RelationshipsDto.fromJson(json['relationships'] as Map<String, dynamic>)
          : null,
      pools: poolsList,
      isFavorited: json['is_favorited'] == true,
    );
  }
}

class E621FileDto {
  final int width;
  final int height;
  final String ext;
  final int size;
  final String md5;
  final String? url;

  E621FileDto({
    required this.width,
    required this.height,
    required this.ext,
    required this.size,
    required this.md5,
    this.url,
  });

  factory E621FileDto.fromJson(Map<String, dynamic> json) {
    return E621FileDto(
      width: (json['width'] as num?)?.toInt() ?? 0,
      height: (json['height'] as num?)?.toInt() ?? 0,
      ext: json['ext']?.toString() ?? 'jpg',
      size: (json['size'] as num?)?.toInt() ?? 0,
      md5: json['md5']?.toString() ?? '',
      url: json['url']?.toString(),
    );
  }
}

class E621PreviewDto {
  final int width;
  final int height;
  final String? url;

  E621PreviewDto({
    required this.width,
    required this.height,
    this.url,
  });

  factory E621PreviewDto.fromJson(Map<String, dynamic> json) {
    return E621PreviewDto(
      width: (json['width'] as num?)?.toInt() ?? 0,
      height: (json['height'] as num?)?.toInt() ?? 0,
      url: json['url']?.toString(),
    );
  }
}

class E621SampleDto {
  final bool has;
  final int? width;
  final int? height;
  final String? url;

  E621SampleDto({
    required this.has,
    this.width,
    this.height,
    this.url,
  });

  factory E621SampleDto.fromJson(Map<String, dynamic> json) {
    return E621SampleDto(
      has: json['has'] == true,
      width: (json['width'] as num?)?.toInt(),
      height: (json['height'] as num?)?.toInt(),
      url: json['url']?.toString(),
    );
  }
}

class E621ScoreDto {
  final int up;
  final int down;
  final int total;

  E621ScoreDto({
    required this.up,
    required this.down,
    required this.total,
  });

  factory E621ScoreDto.fromJson(Map<String, dynamic> json) {
    return E621ScoreDto(
      up: (json['up'] as num?)?.toInt() ?? 0,
      down: (json['down'] as num?)?.toInt() ?? 0,
      total: (json['total'] as num?)?.toInt() ?? 0,
    );
  }
}

class E621TagsDto {
  final List<String> general;
  final List<String> artist;
  final List<String> copyright;
  final List<String> character;
  final List<String> species;
  final List<String> meta;
  final List<String> lore;

  E621TagsDto({
    required this.general,
    required this.artist,
    required this.copyright,
    required this.character,
    required this.species,
    required this.meta,
    required this.lore,
  });

  factory E621TagsDto.fromJson(Map<String, dynamic> json) {
    List<String> getList(String key) {
      final list = json[key] as List<dynamic>?;
      return list?.map((e) => e.toString()).toList() ?? [];
    }

    return E621TagsDto(
      general: getList('general'),
      artist: getList('artist'),
      copyright: getList('copyright'),
      character: getList('character'),
      species: getList('species'),
      meta: getList('meta'),
      lore: getList('lore'),
    );
  }
}

class E621RelationshipsDto {
  final int? parentId;
  final bool hasChildren;
  final bool hasActiveChildren;
  final List<int> children;

  E621RelationshipsDto({
    this.parentId,
    required this.hasChildren,
    required this.hasActiveChildren,
    required this.children,
  });

  factory E621RelationshipsDto.fromJson(Map<String, dynamic> json) {
    final chList = (json['children'] as List<dynamic>?)?.map((e) => (e as num).toInt()).toList() ?? [];
    return E621RelationshipsDto(
      parentId: (json['parent_id'] as num?)?.toInt(),
      hasChildren: json['has_children'] == true,
      hasActiveChildren: json['has_active_children'] == true,
      children: chList,
    );
  }
}

class E621PoolDto {
  final int id;
  final String name;
  final DateTime createdAt;
  final DateTime updatedAt;
  final String description;
  final bool isActive;
  final String category;
  final int postCount;
  final List<int> postIds;

  E621PoolDto({
    required this.id,
    required this.name,
    required this.createdAt,
    required this.updatedAt,
    required this.description,
    required this.isActive,
    required this.category,
    required this.postCount,
    required this.postIds,
  });

  factory E621PoolDto.fromJson(Map<String, dynamic> json) {
    DateTime parseDate(dynamic d) {
      if (d == null) return DateTime.now().toUtc();
      try {
        return DateTime.parse(d.toString());
      } catch (_) {
        return DateTime.now().toUtc();
      }
    }

    final ids = (json['post_ids'] as List<dynamic>?)?.map((e) => (e as num).toInt()).toList() ?? [];

    return E621PoolDto(
      id: (json['id'] as num?)?.toInt() ?? 0,
      name: json['name']?.toString() ?? '',
      createdAt: parseDate(json['created_at']),
      updatedAt: parseDate(json['updated_at']),
      description: json['description']?.toString() ?? '',
      isActive: json['is_active'] == true,
      category: json['category']?.toString() ?? 'series',
      postCount: (json['post_count'] as num?)?.toInt() ?? 0,
      postIds: ids,
    );
  }
}

class E621VoteResponseDto {
  final int? ourScore;
  final E621ScoreDto? score;

  E621VoteResponseDto({this.ourScore, this.score});

  factory E621VoteResponseDto.fromJson(Map<String, dynamic> json) {
    return E621VoteResponseDto(
      ourScore: (json['our_score'] as num?)?.toInt(),
      score: json['score'] != null ? E621ScoreDto.fromJson(json['score'] as Map<String, dynamic>) : null,
    );
  }
}

class E621TagAutocompleteDto {
  final int? id;
  final String name;
  final int postCount;
  final int category;
  final String? antecedentName;

  E621TagAutocompleteDto({
    this.id,
    required this.name,
    required this.postCount,
    required this.category,
    this.antecedentName,
  });

  factory E621TagAutocompleteDto.fromJson(Map<String, dynamic> json) {
    return E621TagAutocompleteDto(
      id: (json['id'] as num?)?.toInt(),
      name: json['name']?.toString() ?? '',
      postCount: (json['post_count'] as num?)?.toInt() ?? 0,
      category: (json['category'] as num?)?.toInt() ?? 0,
      antecedentName: json['antecedent_name']?.toString(),
    );
  }
}
