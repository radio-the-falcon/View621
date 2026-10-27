class SavedSearchEntry {
  final String id;
  final String displayTitle;
  final String effectiveQuery;
  final String? sortKey;
  final bool isSortReversed;
  int? cachedCount;
  final DateTime createdAt;

  SavedSearchEntry({
    required this.id,
    required this.displayTitle,
    required this.effectiveQuery,
    this.sortKey,
    this.isSortReversed = false,
    this.cachedCount,
    DateTime? createdAt,
  }) : createdAt = createdAt ?? DateTime.now().toUtc();

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'display_title': displayTitle,
      'effective_query': effectiveQuery,
      'sort_key': sortKey,
      'is_sort_reversed': isSortReversed,
      'cached_count': cachedCount,
      'created_at': createdAt.toIso8601String(),
    };
  }

  factory SavedSearchEntry.fromJson(Map<String, dynamic> json) {
    DateTime parseDate(dynamic d) {
      if (d == null) return DateTime.now().toUtc();
      try {
        return DateTime.parse(d.toString());
      } catch (_) {
        return DateTime.now().toUtc();
      }
    }

    return SavedSearchEntry(
      id: json['id']?.toString() ?? DateTime.now().millisecondsSinceEpoch.toString(),
      displayTitle: json['display_title']?.toString() ?? json['displayTitle']?.toString() ?? '',
      effectiveQuery: json['effective_query']?.toString() ?? json['effectiveQuery']?.toString() ?? '',
      sortKey: json['sort_key']?.toString() ?? json['sortKey']?.toString(),
      isSortReversed: json['is_sort_reversed'] == true || json['isSortReversed'] == true,
      cachedCount: (json['cached_count'] as num?)?.toInt() ?? (json['cachedCount'] as num?)?.toInt(),
      createdAt: parseDate(json['created_at'] ?? json['createdAt']),
    );
  }
}
