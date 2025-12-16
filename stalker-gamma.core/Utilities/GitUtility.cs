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

    public void CloneGitRepo(string outputDir, string repoUrl)
    {
        try
        {
            // Clone without checkout so we can set config that affects the working tree
            Repository.Clone(repoUrl, outputDir, new CloneOptions { Checkout = false });

            using var repo = new Repository(outputDir);

            // Force CRLF in the working directory on checkout (subject to .gitattributes rules).
            repo.Config.Set("core.autocrlf", "true");
            repo.Config.Set("core.eol", "crlf");

            var defaultBranch = GetDefaultBranch(outputDir);

            // Now populate the working tree using the configured line-ending behavior
            Commands.Checkout(
                repo,
                defaultBranch,
                new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force }
            );
        }
        catch (Exception e)
        {
            throw new GitUtilityException(
                $"""
                Clone
                {outputDir}
                {repoUrl}
                """,
                e
            );
        }
    }

    public void PullGitRepo(string repoDir)
    {
        try
        {
            var repo = new Repository(repoDir);
            Commands.Pull(
                repo,
                MySig,
                new PullOptions
                {
                    MergeOptions = new MergeOptions
                    {
                        FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                    },
                }
            );
        }
        catch (Exception e)
        {
            throw new GitUtilityException(
                $"""
                Pull
                {repoDir}
                """,
                e
            );
        }
    }

    public void CheckoutBranch(string repoDir, string branch)
    {
        var repo = new Repository(repoDir);
        Commands.Checkout(
            repo,
            branch,
            new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force }
        );
    }

    public string GetDefaultBranch(string repoDir)
    {
        var repo = new Repository(repoDir);
        var foundRef = repo.Refs.FirstOrDefault(x => x.CanonicalName == "refs/remotes/origin/HEAD");
        return foundRef is null
            ? throw new GitUtilityException(
                $"""
                Could not find default branch in {repoDir}
                Found {string.Join(", ", repo.Refs.Select(x => x.CanonicalName))}
                """
            )
            : foundRef.TargetIdentifier.Replace("refs/remotes/origin/", "");
    }

    private static readonly Signature MySig = new(
        "stalker-gamma-clone",
        "stalker-gamma-clone@github.com",
        DateTimeOffset.Now
    );
}

public class GitUtilityException : Exception
{
    public GitUtilityException(string msg)
        : base(msg) { }

    public GitUtilityException(string msg, Exception innerException)
        : base(msg, innerException) { }
}
