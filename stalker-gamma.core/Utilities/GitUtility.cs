using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.EventStream;
using CliWrap.Exceptions;

namespace stalker_gamma.core.Utilities;

public partial class GitUtility
{
    public async Task UpdateGitRepo(
        string dir,
        string repoName,
        string repoUrl,
        string branch,
        Action<double> onProgress
    )
    {
        var repoPath = Path.Combine(dir, "resources", repoName);
        var resourcesPath = Path.Combine(dir, "resources");
        var addSafeDir = new[] { "config", "--add", "safe.directory", "'*'" };
        var longPaths = new[] { "config", "core.longpaths", "true" };
        var postBuffer = new[] { "config", "http.postBuffer", "524288000" };
        var maxRequestBuffer = new[] { "config", "http.maxRequestBuffer", "524288000" };

        if (Directory.Exists(repoPath))
        {
            await RunGitCommand(
                repoPath,
                [
                    addSafeDir,
                    longPaths,
                    postBuffer,
                    maxRequestBuffer,
                    ["reset", "--hard", "HEAD"],
                    ["clean", "-f", "-d"],
                ]
            );

            await RunGitCommandWithProgressAsync(repoPath, onProgress, "pull", "--progress");

            await RunGitCommand(
                repoPath,
                [
                    ["checkout", branch],
                ]
            );
        }
        else
        {
            await RunGitCommand(
                resourcesPath,
                [addSafeDir, longPaths, postBuffer, maxRequestBuffer, ["clone", repoUrl]]
            );
            await RunGitCommand(
                repoPath,
                [addSafeDir, longPaths, postBuffer, maxRequestBuffer, ["checkout", branch]]
            );
        }
    }

    public async Task<string> RunGitCommandObs(string workingDir, params string[] args)
    {
        var sb = new StringBuilder();
        var cmd = Cli.Wrap(OsGitPath)
            .WithArguments(argBuilder =>
            {
                foreach (var arg in args)
                {
                    argBuilder.Add(arg);
                }
            })
            .WithWorkingDirectory(workingDir)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb));
        await cmd.ExecuteAsync();
        return sb.ToString();
    }

    private async Task RunGitCommandWithProgressAsync(
        string workingDir,
        Action<double> onProgress,
        params string[] args
    )
    {
        await Cli.Wrap(OsGitPath)
            .WithArguments(argBuilder =>
            {
                foreach (var arg in args)
                {
                    argBuilder.Add(arg);
                }
            })
            .WithWorkingDirectory(workingDir)
            .Observe()
            .ForEachAsync(cmdEvt =>
            {
                switch (cmdEvt)
                {
                    case StandardOutputCommandEvent:
                        break;
                    case StandardErrorCommandEvent stdErr:
                        if (
                            ProgressRx().IsMatch(stdErr.Text)
                            && double.TryParse(
                                ProgressRx().Match(stdErr.Text).Groups[1].Value,
                                out var parsed
                            )
                        )
                        {
                            onProgress(parsed);
                        }

                        break;
                    case ExitedCommandEvent:
                        break;
                    case StartedCommandEvent:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(cmdEvt));
                }
            });
    }

    private async Task RunGitCommand(string workingDir, List<string[]> commands)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        try
        {
            foreach (var command in commands)
            {
                await Cli.Wrap(OsGitPath)
                    .WithArguments(argBuilder =>
                    {
                        foreach (var arg in command)
                        {
                            argBuilder.Add(arg);
                        }
                    })
                    .WithWorkingDirectory(workingDir)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                    .ExecuteAsync();
            }
        }
        catch (CommandExecutionException e)
        {
            throw new GitException(
                $"""
                ERROR EXECUTING GIT COMMAND
                stdout: {stdOut}
                stderr: {stdErr}
                {e}
                """
            );
        }
    }

    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private static readonly string OsGitPath = OperatingSystem.IsWindows()
        ? Path.Join(Dir, "resources", "bin", "git.exe")
        : "git";

    private static string PartJoin(params string[] parts) =>
        string.Join(
            ' ',
            parts.Select(p =>
                $"{(OperatingSystem.IsWindows() ? "" : "")}{p}{(OperatingSystem.IsWindows() ? "" : "")}"
            )
        );

    [GeneratedRegex(@"Receiving objects.*(\d+(\.\d+)?)\s*%", RegexOptions.Compiled)]
    private static partial Regex ProgressRx();
}

public class GitException(string message) : Exception(message);
