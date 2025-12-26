using System.Threading.Tasks;
using stalker_gamma.core.Utilities;

namespace stalker_gamma_gui.ViewModels.Tabs.GammaUpdatesTab.Queries;

public static class GetGitDiffFile
{
    public sealed record Query(string Dir, string PathToFile);

    public sealed class Handler(GitUtility gu)
    {
        public async Task<string> Execute(Query q) =>
            (
                await gu.ExecuteGitCmdAsync(q.Dir, ["diff", "main", "origin/main", q.PathToFile])
            ).StdOut;
    }
}
