using System.Text;
using CliWrap;
using CliWrap.EventStream;
using stalker_gamma.core.Services;

namespace stalker_gamma.core.Utilities;

public class GitUtility(ProgressService progressService)
{
    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public async Task UpdateGitRepo(string dir, string repoName, string repoUrl, string branch)
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
            await RunGitCommand(
                repoPath,
                [
                    .. gitConfig,
                    .. $"reset --hard HEAD && clean -f -d && pull && checkout {branch}".Split(
                        "&&",
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    ),
                ]
            );
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
}

public class GitException(string message) : Exception(message);
