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
        var gitConfig = PartJoin(
            "config --add safe.directory '*' && config core.longpaths true && config http.postBuffer 524288000 && config http.maxRequestBuffer 524288000".Split(
                "&&",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        );

        if (Directory.Exists(repoPath))
        {
            await RunGitCommand(
                repoPath,
                [gitConfig, PartJoin("reset,", "--hard", "HEAD"), PartJoin("clean", "-f", "-d")]
            );

            await RunGitCommandWithProgressAsync(
                repoPath,
                PartJoin("pull", "--progress"),
                onProgress
            );

            await RunGitCommand(repoPath, [PartJoin("checkout", branch)]);
        }
        else
        {
            await RunGitCommand(resourcesPath, [gitConfig, PartJoin("clone", repoUrl)]);
            await RunGitCommand(repoPath, [gitConfig, PartJoin("checkout", branch)]);
        }
    }

    public async Task<string> RunGitCommandObs(string workingDir, string commands)
    {
        commands = PartJoin(OsGitPath, commands);
        var sb = new StringBuilder();
        var cmd = Cli.Wrap(OsShell)
            .WithArguments(argBuilder => argBuilder.Add(OsShellArgs).Add(commands))
            .WithWorkingDirectory(workingDir)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb));
        await cmd.ExecuteAsync();
        return sb.ToString();
    }

    private async Task RunGitCommandWithProgressAsync(
        string workingDir,
        string command,
        Action<double> onProgress
    )
    {
        command = PartJoin(OsGitPath, command);
        await Cli.Wrap(OsShell)
            .WithArguments(argBuilder => argBuilder.Add(OsShellArgs).Add(command))
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

    private async Task RunGitCommand(string workingDir, string[] commands)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        try
        {
            foreach (var command in commands)
            {
                var cmd = PartJoin(OsGitPath, command);
                await Cli.Wrap(OsShell)
                    .WithArguments(argBuilder => argBuilder.Add(OsShellArgs).Add(cmd))
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
    private static readonly string OsShell = OperatingSystem.IsWindows() ? "cmd.exe" : "bash";
    private static readonly string OsShellArgs = OperatingSystem.IsWindows() ? "/C" : "-c";
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
