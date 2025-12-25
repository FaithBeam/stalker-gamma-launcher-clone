using System.Threading.Tasks;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs.Queries;

public static class GetStalkerGammaLastCommit
{
    public record Query(string Dir);

    public sealed class Handler(GitUtility gu)
    {
        public async Task<string> ExecuteAsync(Query q) =>
            (await gu.ExecuteGitCmdAsync(q.Dir, ["rev-parse", "HEAD"])).StdOut.Trim();
    }
}
