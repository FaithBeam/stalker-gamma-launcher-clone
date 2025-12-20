using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Builders;
using CliWrap.EventStream;
using stalker_gamma.core.Models;

namespace stalker_gamma.core.Utilities;

public static partial class ArchiveUtility
{
    public static async Task<StdOutStdErrOutput> ExtractAsync(
        string archivePath,
        string destinationFolder,
        Action<double>? onProgress = null,
        Action<string>? txtProgress = null,
        string? workingDirectory = null,
        CancellationToken? cancellationToken = null
    )
    {
        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        var (exe, args) =
            OperatingSystem.IsMacOS() ? ("tar", ["-xvf", archivePath, "-C", destinationFolder]) // tar macos
            : OperatingSystem.IsLinux()
                ? (
                    Path.Join("resources", "7zip", "linux", "7zzs"),
                    new[] { "x", "-y", "-bsp1", archivePath, $"-o{destinationFolder}" }
                ) // linux 7z
            : (PathTo7Z, ["x", "-y", "-bsp1", archivePath, $"-o{destinationFolder}"]); // windows 7z

        return await ExecuteArchiverCmdAsync(
            exe,
            args,
            onProgress,
            txtProgress,
            workingDirectory: workingDirectory,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="paths">The paths to add to the archive</param>
    /// <param name="destination">The output path</param>
    /// <param name="compressor"></param>
    /// <param name="compressionLevel"></param>
    /// <param name="exclusions">Folders/items to exclude</param>
    /// <param name="workDirectory"></param>
    /// <param name="txtProgress"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<StdOutStdErrOutput> Archive(
        string[] paths,
        string destination,
        string compressor,
        string compressionLevel,
        string[]? exclusions = null,
        string? workDirectory = null,
        Action<string>? txtProgress = null,
        CancellationToken? cancellationToken = null
    )
    {
        var args = new[]
        {
            "a",
            "-bsp",
            $"{destination}",
            $"{string.Join(" ", paths.Select(x => $"{x}"))}",
            $"-m0={(compressor == "zstd" ? "bcj" : compressor)}",
            $"{(compressor == "zstd" ? "-m1=zstd " : "")}",
            $"-mx{compressionLevel}",
            $"{(exclusions?.Length == 0 ? "" : string.Join(" ", exclusions!.Select(x => $"-xr!{x}")))}",
        };

        return await ExecuteArchiverCmdAsync(
            PathTo7Z,
            args,
            workingDirectory: workDirectory,
            txtProgress: txtProgress,
            cancellationToken: cancellationToken
        );
    }

    private static async Task<StdOutStdErrOutput> ExecuteArchiverCmdAsync(
        string exe,
        string[] args,
        Action<double>? onProgress = null,
        Action<string>? txtProgress = null,
        string? workingDirectory = null,
        CancellationToken? cancellationToken = null
    )
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        try
        {
            var cmd = Cli.Wrap(exe)
                .WithArguments(argBuilder => AppendArgument(args, argBuilder))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                .WithWorkingDirectory(workingDirectory ?? "");

            if (onProgress is null && txtProgress is null)
            {
                await cmd.ExecuteAsync(cancellationToken ?? CancellationToken.None);
            }
            else
            {
                void WriteProgress(StandardOutputCommandEvent stdOutEvt)
                {
                    if (
                        onProgress is not null
                        && ProgressRx().IsMatch(stdOutEvt.Text)
                        && double.TryParse(
                            ProgressRx().Match(stdOutEvt.Text).Groups[1].Value,
                            out var parsed
                        )
                    )
                    {
                        onProgress(parsed);
                    }

                    txtProgress?.Invoke(stdOutEvt.Text);
                }

                await cmd.Observe(cancellationToken ?? CancellationToken.None)
                    .ForEachAsync(cmdEvt =>
                    {
                        switch (cmdEvt)
                        {
                            case StandardOutputCommandEvent stdOutEvt:
                                WriteProgress(stdOutEvt);
                                break;
                        }
                    });
            }
        }
        catch (Exception e)
        {
            throw new ArchiveUtilityException(
                $"""
                Error executing {exe}
                {string.Join(' ', args)}
                StdOut: {stdOut}
                StdErr: {stdErr}
                Exception: {e}
                """,
                e
            );
        }

        return new StdOutStdErrOutput(stdOut.ToString(), stdErr.ToString());
    }

    private static void AppendArgument(string[] args, ArgumentsBuilder argBuilder)
    {
        foreach (var arg in args)
        {
            argBuilder.Add(arg);
        }
    }

    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private static readonly string PathTo7Z = Path.Join(Dir, "resources", "7zip", "7z.exe");

    [GeneratedRegex(@"(\d+(\.\d+)?)\s*%", RegexOptions.Compiled)]
    private static partial Regex ProgressRx();
}

public class ArchiveUtilityException(string msg, Exception innerException)
    : Exception(msg, innerException);
