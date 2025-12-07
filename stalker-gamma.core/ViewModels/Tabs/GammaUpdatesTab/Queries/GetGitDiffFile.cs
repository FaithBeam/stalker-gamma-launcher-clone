using LibGit2Sharp;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Queries;

public static class GetGitDiffFile
{
    public sealed record Query(string Dir, string PathToFile);

    public sealed class Handler
    {
        public string Execute(Query q)
        {
            var repo = new Repository(q.Dir);
            var diff = repo.Diff.Compare<Patch>(
                repo.Head.Tip.Tree,
                repo.Branches["origin/main"].Tip.Tree,
                [q.PathToFile]
            );
            return diff.Content;
        }
    }
}
