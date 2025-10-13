using stalker_gamma.core.Utilities;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Models;

namespace stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Queries;

public static class GetGitDiff
{
    public sealed record Query(string Dir);

    public sealed class Handler(GitUtility gu)
    {
        public async Task<List<GitDiff>> ExecuteAsync(Query q)
        {
            gu.RunGitCommandObs(q.Dir, "config diff.renameLimit 999999").GetAwaiter().GetResult();
            return (await gu.RunGitCommandObs(q.Dir, "diff main origin/main --name-status"))
                .Trim()
                .Split("\n")
                .Select(x =>
                {
                    var split = x.Split("\t");
                    var diffType = split[0][0] switch
                    {
                        'M' => GitDiffType.Modified,
                        'A' => GitDiffType.Added,
                        'D' => GitDiffType.Deleted,
                        'R' => GitDiffType.Renamed,
                        _ => throw new ArgumentOutOfRangeException($"{split[0][0]}"),
                    };
                    return new GitDiff(diffType, split[1]);
                })
                .ToList();
        }
    }
}
