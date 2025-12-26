using System.Threading.Tasks;
using stalker_gamma.core.Utilities;

namespace stalker_gamma_gui.ViewModels.Tabs.Queries;

public static class GetStalkerGammaLastCommit
{
    public record Query(string Dir);

    public sealed class Handler
    {
        public async Task<string> ExecuteAsync(Query q) =>
            (await GitUtility.ExecuteGitCmdAsync(q.Dir, ["rev-parse", "HEAD"])).StdOut.Trim();
    }
}
