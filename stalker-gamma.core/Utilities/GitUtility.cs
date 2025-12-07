using LibGit2Sharp;
using stalker_gamma.core.ViewModels.Tabs.MainTab;

namespace stalker_gamma.core.Utilities;

public partial class GitUtility
{
    public void UpdateGitRepo(
        string dir,
        string repoName,
        string branch,
        Action<double> onProgress,
        Action<Status> onStatus
    )
    {
        var repoPath = Path.Combine(dir, "resources", repoName);

        var repo = new Repository(repoPath);
        repo.Config.Add("safe.directory", "*");
        repo.Config.Set("core.longpaths", "true");
        repo.Config.Set("http.postBuffer", "524288000");
        repo.Config.Set("http.maxRequestBuffer", "524288000");

        repo.Reset(ResetMode.Hard);

        Commands.Pull(
            repo,
            MySig,
            new PullOptions
            {
                FetchOptions = new FetchOptions
                {
                    OnProgress = serverProgressOutput =>
                    {
                        onStatus(Status.Checking);
                        return true;
                    },
                    OnTransferProgress = progHandler =>
                    {
                        onStatus(Status.Downloading);
                        onProgress(
                            (double)progHandler.ReceivedObjects / progHandler.TotalObjects * 100
                        );
                        return true;
                    },
                },
            }
        );

        Commands.Checkout(repo, branch);
    }

    private static readonly Signature MySig = new(
        "stalker-gamma-clone",
        "stalker-gamma-clone@github.com",
        DateTimeOffset.Now
    );
}
