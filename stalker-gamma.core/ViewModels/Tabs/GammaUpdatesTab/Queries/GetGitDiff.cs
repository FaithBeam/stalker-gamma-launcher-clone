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
            await gu.RunGitCommandObs(q.Dir, "config diff.renameLimit 999999");
            return (await gu.RunGitCommandObs(q.Dir, "diff main origin/main --name-status"))
                .Trim()
                .Split("\n")
                .Select(x => x.Split("\t"))
                .Where(x => x.Length == 2 && !string.IsNullOrWhiteSpace(x[0]))
                .Select(x =>
                {
                    var diffType = x[0][0] switch
                    {
                        'M' => GitDiffType.Modified,
                        'A' => GitDiffType.Added,
                        'D' => GitDiffType.Deleted,
                        'R' => GitDiffType.Renamed,
                        _ => throw new ArgumentOutOfRangeException($"{x[0][0]}"),
                    };
                    return new GitDiff(diffType, x[1]);
                })
                .ToList();
        }
    }
}
