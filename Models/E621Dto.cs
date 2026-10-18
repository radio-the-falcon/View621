using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace E6Viewer.Models;

public class E621PostsResponse
{
    [JsonPropertyName("posts")]
    public List<E621PostDto> Posts { get; set; } = new();
}

public class E621SinglePostResponse
{
    [JsonPropertyName("post")]
    public E621PostDto? Post { get; set; }
}

public class E621PostDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("file")]
    public E621FileDto? File { get; set; }

    [JsonPropertyName("preview")]
    public E621PreviewDto? Preview { get; set; }

    [JsonPropertyName("sample")]
    public E621SampleDto? Sample { get; set; }

    [JsonPropertyName("score")]
    public E621ScoreDto? Score { get; set; }

    [JsonPropertyName("tags")]
    public E621TagsDto? Tags { get; set; }

    [JsonPropertyName("rating")]
    public string Rating { get; set; } = "s";

    [JsonPropertyName("fav_count")]
    public int FavCount { get; set; }

    [JsonPropertyName("sources")]
    public List<string>? Sources { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("relationships")]
    public E621RelationshipsDto? Relationships { get; set; }

    [JsonPropertyName("pools")]
    public List<long>? Pools { get; set; }
}

public class E621RelationshipsDto
{
    [JsonPropertyName("parent_id")]
    public long? ParentId { get; set; }

    [JsonPropertyName("has_children")]
    public bool HasChildren { get; set; }

    [JsonPropertyName("has_active_children")]
    public bool HasActiveChildren { get; set; }

    [JsonPropertyName("children")]
    public List<long>? Children { get; set; }
}

public class E621PoolDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "series";

    [JsonPropertyName("post_count")]
    public int PostCount { get; set; }

    [JsonPropertyName("post_ids")]
    public List<long> PostIds { get; set; } = new();
}

public class E621FileDto
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("ext")]
    public string Ext { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class E621PreviewDto
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class E621SampleDto
{
    [JsonPropertyName("has")]
    public bool Has { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class E621ScoreDto
{
    [JsonPropertyName("up")]
    public int Up { get; set; }

    [JsonPropertyName("down")]
    public int Down { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class E621TagsDto
{
    [JsonPropertyName("general")]
    public List<string>? General { get; set; }

    [JsonPropertyName("artist")]
    public List<string>? Artist { get; set; }

    [JsonPropertyName("copyright")]
    public List<string>? Copyright { get; set; }

    [JsonPropertyName("character")]
    public List<string>? Character { get; set; }

    [JsonPropertyName("species")]
    public List<string>? Species { get; set; }

    [JsonPropertyName("meta")]
    public List<string>? Meta { get; set; }

    [JsonPropertyName("lore")]
    public List<string>? Lore { get; set; }
}

public class E621TagAutocompleteDto
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("post_count")]
    public int PostCount { get; set; }

    [JsonPropertyName("category")]
    public int Category { get; set; }

    [JsonPropertyName("antecedent_name")]
    public string? AntecedentName { get; set; }
}
