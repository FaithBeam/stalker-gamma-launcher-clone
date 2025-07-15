using System.Text;
using CliWrap;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.Utilities;

public class GitUtility(ProgressService progressService)
{
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

    public async Task RunGitCommand(string workingDir, string[] commands)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var gitCmdPath = Path.GetFullPath(Path.Join("resources", "bin", "git.exe"));
                foreach (var command in commands)
                {
                    await Cli.Wrap(gitCmdPath)
                        .WithArguments(command)
                        .WithWorkingDirectory(workingDir)
                        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                        .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                        .ExecuteAsync();
                }
            }
            else
            {
                foreach (var command in commands)
                {
                    await Cli.Wrap("git")
                        .WithArguments(command)
                        .WithWorkingDirectory(workingDir)
                        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                        .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                        .ExecuteAsync();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine(stdOut);
            Console.WriteLine(stdErr);
            throw;
        }
    }
}
