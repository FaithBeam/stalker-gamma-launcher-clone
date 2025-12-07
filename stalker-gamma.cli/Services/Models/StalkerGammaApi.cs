using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace stalker_gamma.cli.Services.Models;

[JsonSerializable(typeof(StalkerGammaApiResponse))]
public partial class StalkerGammaApiCtx : JsonSerializerContext;

public class StalkerGammaApiResponse
{
    [JsonPropertyName("initialised")]
    public bool Initialised { get; set; }

    [JsonPropertyName("main")]
    public Main[] Main { get; set; } = [];

    [JsonPropertyName("dev")]
    public Dev[] Dev { get; set; } = [];

    [JsonPropertyName("lastUpdate")]
    public string? LastUpdate { get; set; }
}

public partial class Main
{
    [JsonPropertyName("instructions")]
    public string[] Instructions { get; set; } = [];

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("license")]
    public string? License { get; set; }

    [JsonPropertyName("uploader")]
    public string? Uploader
    {
        get => NonWordCharRx().Replace(field ?? "", "");
        set;
    }

    [JsonPropertyName("uploaderProfileUrl")]
    public string? UploaderProfileUrl { get; set; }

    [JsonPropertyName("md5Hash")]
    public string? Md5Hash { get; set; }

    [JsonPropertyName("credits")]
    public string? Credits { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = [];

    [JsonPropertyName("mirrorsUrl")]
    public string? MirrorsUrl { get; set; }

    [JsonPropertyName("addonId")]
    public int? AddonId { get; set; }

    [JsonPropertyName("rating")]
    public double Rating { get; set; }

    [JsonPropertyName("ratings")]
    public int Ratings { get; set; }

    [JsonPropertyName("downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("fileSize")]
    public int FileSize { get; set; }

    [JsonPropertyName("uploadedAt")]
    public string? UploadedAt { get; set; }

    [JsonPropertyName("lastUpdatedAt")]
    public string? LastUpdatedAt { get; set; }

    [GeneratedRegex("\\W")]
    private partial Regex NonWordCharRx();
}

public class Dev
{
    [JsonPropertyName("instructions")]
    public string[] Instructions { get; set; } = [];

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("license")]
    public string? License { get; set; }

    [JsonPropertyName("uploader")]
    public string? Uploader { get; set; }

    [JsonPropertyName("uploaderProfileUrl")]
    public string? UploaderProfileUrl { get; set; }

    [JsonPropertyName("md5Hash")]
    public string? Md5Hash { get; set; }

    [JsonPropertyName("credits")]
    public string? Credits { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = [];

    [JsonPropertyName("mirrorsUrl")]
    public string? MirrorsUrl { get; set; }

    [JsonPropertyName("addonId")]
    public int AddonId { get; set; }

    [JsonPropertyName("rating")]
    public double Rating { get; set; }

    [JsonPropertyName("ratings")]
    public int Ratings { get; set; }

    [JsonPropertyName("downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("fileSize")]
    public int FileSize { get; set; }

    [JsonPropertyName("uploadedAt")]
    public string? UploadedAt { get; set; }

    [JsonPropertyName("lastUpdatedAt")]
    public string? LastUpdatedAt { get; set; }
}
