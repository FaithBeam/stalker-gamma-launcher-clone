using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.MainWindow.SwitchToMyFork;

public static class SwitchToMyFork
{
    public sealed class Handler(GitUtility gu)
    {
        private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

        public async Task ExecuteAsync()
        {
            var repoPath = Path.Combine(_dir, "resources", "Stalker_GAMMA");

            var curRemote = await gu.RunGitCommandObs(repoPath, "remote -v");
            // check if the current repo still needs to be updated
            if (curRemote.Contains("https://github.com/Grokitach/Stalker_GAMMA"))
            {
                await gu.RunGitCommand(
                    repoPath,
                    [
                        "remote remove origin",
                        "remote add origin https://github.com/FaithBeam/Stalker_GAMMA",
                        "reset --hard",
                        "checkout main",
                        "fetch",
                        "branch --set-upstream-to=origin/main main",
                    ]
                );
            }
        }
    }
}
