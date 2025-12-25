using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Builders;
using stalker_gamma.core.Models;

namespace stalker_gamma.core.Utilities;

public partial class GitUtility
{
    public async Task ConfigSetAsync(string repoPath, string configName, string configVal) =>
        await ExecuteGitCmdAsync(repoPath, ["config", "set", configName, configVal]);

    public async Task UpdateGitRepoAsync(
        string dir,
        string repoName,
        string branch,
        Action<double> onProgress
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
        await ExecuteGitCmdAsync(repoPath, ["pull"], onProgress: onProgress);
        await CheckoutBranch(repoPath, branch);
    }

    public async Task<StdOutStdErrOutput> CloneGitRepo(
        string outputDir,
        string repoUrl,
        Action<double>? onProgress = null
    ) => await ExecuteGitCmdAsync("", ["clone", repoUrl, outputDir], onProgress: onProgress);

    public async Task<StdOutStdErrOutput> PullGitRepo(
        string pathToRepo,
        Action<double>? onProgress = null
    ) => await ExecuteGitCmdAsync(pathToRepo, ["pull"], onProgress: onProgress);

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

    public async Task EnableLongPathsAsync() =>
        await ExecuteGitCmdAsync("", ["config", "--system", "core.longpaths", "true"]);

    public async Task<StdOutStdErrOutput> ExecuteGitCmdAsync(
        string workingDir,
        string[] args,
        Action<double>? onProgress = null,
        Action<string>? txtProgress = null
    )
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        List<string> finalArgs = [.. args];
        if (onProgress is not null && (args.Contains("clone") || args.Contains("pull")))
        {
            finalArgs.Add("--progress");
        }

        try
        {
            await Cli.Wrap(PathToGit)
                .WithArguments(argBuilder => AppendArgument([.. finalArgs], argBuilder))
                .WithStandardOutputPipe(
                    PipeTarget.Merge(
                        PipeTarget.ToStringBuilder(stdOut),
                        PipeTarget.ToDelegate(line => txtProgress?.Invoke(line))
                    )
                )
                .WithStandardErrorPipe(
                    PipeTarget.Merge(
                        PipeTarget.ToStringBuilder(stdErr),
                        PipeTarget.ToDelegate(line =>
                        {
                            if (onProgress == null)
                            {
                                return;
                            }
                            var match = ProgressRegex().Match(line);
                            if (match.Success)
                            {
                                if (double.TryParse(match.Groups[1].Value, out var progress))
                                {
                                    onProgress(progress / 100);
                                }
                            }
                        })
                    )
                )
                .WithWorkingDirectory(workingDir)
                .ExecuteAsync();
        }
        catch (Exception e)
        {
            throw new GitUtilityException(
                $"""
                Error executing git command
                {workingDir}
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
        ? Path.Join("resources", "git", "cmd", "git.exe")
        : "git";

    [GeneratedRegex(@"Receiving objects:\s*(\d+)%")]
    private static partial Regex ProgressRegex();
}

public class GitUtilityException(string msg, Exception innerException)
    : Exception(msg, innerException);
