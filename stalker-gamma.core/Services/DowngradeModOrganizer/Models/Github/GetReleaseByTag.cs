using System.Text.Json.Serialization;

namespace stalker_gamma.core.Services.DowngradeModOrganizer.Models.Github;

[JsonSerializable(typeof(GetReleaseByTag))]
public partial class GetReleaseByTagCtx : JsonSerializerContext;

public class GetReleaseByTag
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("assets_url")]
    public string? AssetsUrl { get; set; }

    [JsonPropertyName("upload_url")]
    public string? UploadUrl { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("author")]
    public Author? Author { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("target_commitish")]
    public string? TargetCommitish { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("immutable")]
    public bool Immutable { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public Assets[]? Assets { get; set; }

    [JsonPropertyName("tarball_url")]
    public string? TarballUrl { get; set; }

    [JsonPropertyName("zipball_url")]
    public string? ZipballUrl { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("reactions")]
    public Reactions? Reactions { get; set; }
}

public class Author
{
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("gravatar_id")]
    public string? GravatarId { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("followers_url")]
    public string? FollowersUrl { get; set; }

    [JsonPropertyName("following_url")]
    public string? FollowingUrl { get; set; }

    [JsonPropertyName("gists_url")]
    public string? GistsUrl { get; set; }

    [JsonPropertyName("starred_url")]
    public string? StarredUrl { get; set; }

    [JsonPropertyName("subscriptions_url")]
    public string? SubscriptionsUrl { get; set; }

    [JsonPropertyName("organizations_url")]
    public string? OrganizationsUrl { get; set; }

    [JsonPropertyName("repos_url")]
    public string? ReposUrl { get; set; }

    [JsonPropertyName("events_url")]
    public string? EventsUrl { get; set; }

    [JsonPropertyName("received_events_url")]
    public string? ReceivedEventsUrl { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("user_view_type")]
    public string? UserViewType { get; set; }

    [JsonPropertyName("site_admin")]
    public bool SiteAdmin { get; set; }
}

public class Assets
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("label")]
    public object? Label { get; set; }

    [JsonPropertyName("uploader")]
    public Uploader? Uploader { get; set; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("digest")]
    public object? Digest { get; set; }

    [JsonPropertyName("download_count")]
    public int DownloadCount { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}

public class Uploader
{
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("gravatar_id")]
    public string? GravatarId { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("followers_url")]
    public string? FollowersUrl { get; set; }

    [JsonPropertyName("following_url")]
    public string? FollowingUrl { get; set; }

    [JsonPropertyName("gists_url")]
    public string? GistsUrl { get; set; }

    [JsonPropertyName("starred_url")]
    public string? StarredUrl { get; set; }

    [JsonPropertyName("subscriptions_url")]
    public string? SubscriptionsUrl { get; set; }

    [JsonPropertyName("organizations_url")]
    public string? OrganizationsUrl { get; set; }

    [JsonPropertyName("repos_url")]
    public string? ReposUrl { get; set; }

    [JsonPropertyName("events_url")]
    public string? EventsUrl { get; set; }

    [JsonPropertyName("received_events_url")]
    public string? ReceivedEventsUrl { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("user_view_type")]
    public string? UserViewType { get; set; }

    [JsonPropertyName("site_admin")]
    public bool SiteAdmin { get; set; }
}

public class Reactions
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("+1")]
    public int Plus1 { get; set; }

    [JsonPropertyName("-1")]
    public int Minus1 { get; set; }

    [JsonPropertyName("laugh")]
    public int Laugh { get; set; }

    [JsonPropertyName("hooray")]
    public int Hooray { get; set; }

    [JsonPropertyName("confused")]
    public int Confused { get; set; }

    [JsonPropertyName("heart")]
    public int Heart { get; set; }

    [JsonPropertyName("rocket")]
    public int Rocket { get; set; }

    [JsonPropertyName("eyes")]
    public int Eyes { get; set; }
}
