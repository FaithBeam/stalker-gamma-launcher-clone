using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace stalker_gamma.core.ViewModels.Tabs.Queries;

public static class GetGitHubRepoCommits
{
    public record Query(string Owner, string Repo);

    public sealed class Handler
    {
        public async Task<string[]?> ExecuteAsync(Query q)
        {
            var hc = new HttpClient
            {
                DefaultRequestHeaders =
                {
                    UserAgent = { ProductInfoHeaderValue.Parse("stalker-gamma-launcher-clone") },
                },
            };
            return (
                await hc.GetFromJsonAsync<List<RootObject>>(
                    $"https://api.github.com/repos/{q.Owner}/{q.Repo}/commits",
                    jsonTypeInfo: GetGitHubRepoCommitsCtx.Default.ListRootObject
                )
            )
                ?.Select(x => x.sha)
                .ToArray();
        }
    }
}

[JsonSerializable(typeof(List<RootObject>))]
public partial class GetGitHubRepoCommitsCtx : JsonSerializerContext;

public record RootObject(
    string sha,
    string node_id,
    Commit commit,
    string url,
    string html_url,
    string comments_url,
    Author author,
    Committer committer,
    Parents[] parents
);

public record Commit(
    Author1 author,
    Committer1 committer,
    string message,
    Tree tree,
    string url,
    int comment_count,
    Verification verification
);

public record Author1(string name, string email, string date);

public record Committer1(string name, string email, string date);

public record Tree(string sha, string url);

public record Verification(
    bool verified,
    string reason,
    string signature,
    string payload,
    string verified_at
);

public record Author(
    string login,
    int id,
    string node_id,
    string avatar_url,
    string gravatar_id,
    string url,
    string html_url,
    string followers_url,
    string following_url,
    string gists_url,
    string starred_url,
    string subscriptions_url,
    string organizations_url,
    string repos_url,
    string events_url,
    string received_events_url,
    string type,
    string user_view_type,
    bool site_admin
);

public record Committer(
    string login,
    int id,
    string node_id,
    string avatar_url,
    string gravatar_id,
    string url,
    string html_url,
    string followers_url,
    string following_url,
    string gists_url,
    string starred_url,
    string subscriptions_url,
    string organizations_url,
    string repos_url,
    string events_url,
    string received_events_url,
    string type,
    string user_view_type,
    bool site_admin
);

public record Parents(string sha, string url, string html_url);
