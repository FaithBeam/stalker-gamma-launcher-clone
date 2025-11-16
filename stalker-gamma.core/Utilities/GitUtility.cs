using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.EventStream;
using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.Tabs.MainTab;

namespace stalker_gamma.core.Utilities;

public partial class GitUtility(ProgressService progressService)
{
    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public async Task UpdateGitRepo(
        string dir,
        string repoName,
        string repoUrl,
        string branch,
        ModDownloadExtractProgressVm modDownloadExtractProgressVm
    )
    {
        var repoPath = Path.Combine(dir, "resources", repoName);
        var resourcesPath = Path.Combine(dir, "resources");
        var gitConfig =
            "config --global --add safe.directory '*' && config --global core.longpaths true && config --global http.postBuffer 524288000 && config --global http.maxRequestBuffer 524288000".Split(
                "&&",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );

        if (Directory.Exists(repoPath))
        {
            progressService.UpdateProgress($" Updating {repoName.Replace('_', ' ')}.");
            await RunGitCommand(repoPath, [.. gitConfig, "reset --hard HEAD", "clean -f -d"]);

            modDownloadExtractProgressVm.Status = Status.Downloading;
            await RunGitCommandWithProgress(repoPath, "pull")
                .ForEachAsync(cmdEvt =>
                {
                    switch (cmdEvt)
                    {
                        case StandardOutputCommandEvent stdOut:
                            if (
                                ProgressRx().IsMatch(stdOut.Text)
                                && double.TryParse(
                                    ProgressRx().Match(stdOut.Text).Groups[1].Value,
                                    out var parsed
                                )
                            )
                            {
                                modDownloadExtractProgressVm.DownloadProgressInterface.Report(
                                    parsed
                                );
                            }
                            break;
                        case StandardErrorCommandEvent:
                        case ExitedCommandEvent:
                            modDownloadExtractProgressVm.DownloadProgressInterface.Report(100);
                            modDownloadExtractProgressVm.Status = Status.Downloaded;
                            break;
                        case StartedCommandEvent:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(cmdEvt));
                    }
                });

            await RunGitCommand(repoPath, [$"checkout {branch}"]);
        }
        else
        {
            progressService.UpdateProgress(
                $" Cloning {repoName.Replace('_', ' ')} (can take some time)."
            );
            await RunGitCommand(resourcesPath, [.. gitConfig, $"clone {repoUrl}"]);
            await RunGitCommand(repoPath, [.. gitConfig, $"checkout {branch}"]);
        }
    }

    public async Task<string> RunGitCommandObs(
        string workingDir,
        string commands,
        CancellationToken? ct = null
    )
    {
        var sb = new StringBuilder();
        var cmd = Cli.Wrap(GetGitPath)
            .WithArguments(commands)
            .WithWorkingDirectory(workingDir)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb));
        await cmd.ExecuteAsync();
        return sb.ToString();
    }

    public IObservable<CommandEvent> RunGitCommandWithProgress(string workingDir, string command) =>
        Cli.Wrap(GetGitPath).WithArguments(command).WithWorkingDirectory(workingDir).Observe();

    public async Task RunGitCommand(string workingDir, string[] commands)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        try
        {
            foreach (var command in commands)
            {
                await Cli.Wrap(GetGitPath)
                    .WithArguments(command)
                    .WithWorkingDirectory(workingDir)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                    .ExecuteAsync();
            }
        }
        catch (Exception e)
        {
            throw new GitException($"{stdOut}\n{stdErr}\n{e}");
        }
    }

    private static string GetGitPath =>
        OperatingSystem.IsWindows()
            ? Path.Join(Dir, Path.Join("resources", "bin", "git.exe"))
            : "git";

    [GeneratedRegex(@"(\d+(\.\d+)?)\s*%", RegexOptions.Compiled)]
    private static partial Regex ProgressRx();
}

public class GitException(string message) : Exception(message);
