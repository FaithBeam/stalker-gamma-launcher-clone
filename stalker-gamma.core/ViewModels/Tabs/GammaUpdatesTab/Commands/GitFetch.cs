using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Commands;

public static class GitFetch
{
    public sealed record Command(string Dir, bool All = true);

    public sealed class Handler(GitUtility gu)
    {
        public async Task ExecuteAsync(Command c) =>
            await gu.RunGitCommandObs(c.Dir, $"fetch {(c.All ? "--all" : "")}");
    }
}
