using LibGit2Sharp;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Models;

namespace stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Queries;

public static class GetGitDiff
{
    public sealed record Query(string Dir);

    public sealed class Handler
    {
        public List<GitDiff> Execute(Query q)
        {
            var repo = new Repository(q.Dir);
            repo.Config.Set("diff.renameLimit", "999999");
            var changes = repo.Diff.Compare<TreeChanges>(
                repo.Head.Tip.Tree,
                repo.Branches["origin/main"].Tip.Tree,
                new CompareOptions()
            );
            return changes
                .Where(x =>
                    x.Status
                        is ChangeKind.Modified
                            or ChangeKind.Added
                            or ChangeKind.Deleted
                            or ChangeKind.Renamed
                )
                .Select(x => new GitDiff(
                    x.Status switch
                    {
                        ChangeKind.Added => GitDiffType.Added,
                        ChangeKind.Deleted => GitDiffType.Deleted,
                        ChangeKind.Modified => GitDiffType.Modified,
                        ChangeKind.Renamed => GitDiffType.Renamed,
                        _ => throw new ArgumentOutOfRangeException(),
                    },
                    x.Path
                ))
                .ToList();
        }
    }
}
