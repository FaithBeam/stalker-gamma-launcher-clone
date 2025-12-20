using System.Text;
using CliWrap;
using CliWrap.Builders;
using stalker_gamma.core.Models;
using stalker_gamma.core.ViewModels.Tabs.MainTab;

namespace stalker_gamma.core.Utilities;

public partial class GitUtility
{
    public async Task ConfigSetAsync(string repoPath, string configName, string configVal) =>
        await ExecuteGitCmdAsync(repoPath, ["config", "set", configName, configVal]);

    public async Task UpdateGitRepoAsync(
        string dir,
        string repoName,
        string branch,
        Action<double> onProgress,
        Action<Status> onStatus
    )
    {
        var repoPath = Path.Combine(dir, "resources", repoName);

        foreach (
            var cmd in new List<ValueTuple<string, string>>
            {
                ("safe.directory", "*"),
                ("core.longpaths", "true"),
                ("http.postBuffer", "524288000"),
                ("http.maxRequestBuffer", "524288000"),
            }
        )
        {
            await ConfigSetAsync(repoPath, cmd.Item1, cmd.Item2);
        }

        await ExecuteGitCmdAsync(repoPath, ["reset", "--hard"]);
        await ExecuteGitCmdAsync(repoPath, ["pull"]);
        await CheckoutBranch(repoPath, branch);
    }

    public async Task<StdOutStdErrOutput> CloneGitRepo(string outputDir, string repoUrl) =>
        await ExecuteGitCmdAsync("", ["clone", repoUrl, outputDir]);

    public async Task<StdOutStdErrOutput> PullGitRepo(string pathToRepo) =>
        await ExecuteGitCmdAsync(pathToRepo, ["pull"]);

    public async Task<StdOutStdErrOutput> CheckoutBranch(string pathToRepo, string branch) =>
        await ExecuteGitCmdAsync(pathToRepo, ["checkout", branch]);

    public async Task<string> GetDefaultBranch(string pathToRepo)
    {
        var gitCmdResult = await ExecuteGitCmdAsync(
            pathToRepo,
            ["rev-parse", "--abbrev-ref", "origin/HEAD"]
        );
        return gitCmdResult.StdOut.Replace("origin/", "");
    }

    public async Task<StdOutStdErrOutput> ExecuteGitCmdAsync(string pathToRepo, string[] args)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        try
        {
            await Cli.Wrap(PathToGit)
                .WithArguments(argBuilder => AppendArgument(args, argBuilder))
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                .WithWorkingDirectory(pathToRepo)
                .ExecuteAsync();
        }
        catch (Exception e)
        {
            throw new GitUtilityException(
                $"""
                Error executing git command
                {pathToRepo}
                {string.Join(' ', args)}
                StdOut: {stdOut}
                StdErr: {stdErr}
                Exception: {e}
                """,
                e
            );
        }

        return new StdOutStdErrOutput(stdOut.ToString().Trim(), stdErr.ToString().Trim());
    }

    private static void AppendArgument(string[] args, ArgumentsBuilder argBuilder)
    {
        foreach (var arg in args)
        {
            argBuilder.Add(arg);
        }
    }

    private static readonly string PathToGit = OperatingSystem.IsWindows()
        ? Path.Join("resources", "bin", "git.exe")
        : "git";
}

public class GitUtilityException(string msg, Exception innerException)
    : Exception(msg, innerException);
