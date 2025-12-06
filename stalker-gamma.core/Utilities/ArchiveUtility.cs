using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Builders;
using CliWrap.EventStream;
using CliWrap.Exceptions;

namespace stalker_gamma.core.Utilities;

public static partial class ArchiveUtility
{
    public static async Task ExtractAsync(string archivePath, string destinationFolder)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        var args = new[] { "x", $"{archivePath}", "-aoa", $"-o{destinationFolder}" };
        var cmd = Cli.Wrap(Os7ZPath)
            .WithArguments(argBuilder => AppendArgument(args, argBuilder))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr));
        try
        {
            await cmd.ExecuteAsync();
        }
        catch (CommandExecutionException e)
        {
            throw new SevenZipExtractException(
                $"""
                Error extracting archive {archivePath}
                Args: {args}
                StdOut: {stdOut}
                StdErr: {stdErr}
                """,
                e
            );
        }
    }

    public static IObservable<CommandEvent> Extract(
        string archivePath,
        string destinationFolder,
        CancellationToken? ct = null,
        string? workingDirectory = null
    )
    {
        var args = new[] { "x", "-y", $"{archivePath}", $"-o{destinationFolder}" };
        var cmd = Cli.Wrap(Os7ZPath).WithArguments(argBuilder => AppendArgument(args, argBuilder));
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            cmd = cmd.WithWorkingDirectory(workingDirectory);
        }

        try
        {
            return ct is not null ? cmd.Observe(ct.Value) : cmd.Observe();
        }
        catch (CommandExecutionException e)
        {
            throw new SevenZipExtractException(
                $"""
                Error extracting archive {archivePath}
                Args: {args}
                """,
                e
            );
        }
    }

    public static async Task ExtractWithProgress(
        string archivePath,
        string destinationFolder,
        Action<double> onProgress,
        string? workingDirectory = null
    )
    {
        string pathToUnarchiver;
        string[] args;

        if (archivePath.EndsWith(".rar") && OperatingSystem.IsMacOS())
        {
            pathToUnarchiver = OsRarPath;
            args = ["x", "-o+", archivePath, destinationFolder];
        }
        else
        {
            args = ["x", "-y", "-bsp1", archivePath, $"-o\"{destinationFolder}\""];
            pathToUnarchiver = Os7ZPath;
        }

        var cmd = Cli.Wrap(pathToUnarchiver)
            .WithArguments(argBuilder => AppendArgument(args, argBuilder));
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            cmd = cmd.WithWorkingDirectory(workingDirectory);
        }

        try
        {
            await cmd.Observe()
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
                                onProgress(parsed);
                            }
                            break;
                        case ExitedCommandEvent:
                        case StandardErrorCommandEvent:
                        case StartedCommandEvent:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(cmdEvt));
                    }
                });
        }
        catch (CommandExecutionException e)
        {
            throw new SevenZipExtractException(
                $"""
                Error extracting archive {archivePath}
                Args: {args}
                """,
                e
            );
        }
    }

    public static IObservable<CommandEvent> List(string archivePath, CancellationToken? ct = null)
    {
        var args = new[] { "l", "-slt", archivePath };
        var cmd = Cli.Wrap(Os7ZPath).WithArguments(argBuilder => AppendArgument(args, argBuilder));
        return ct is not null ? cmd.Observe(ct.Value) : cmd.Observe();
    }

    private static void AppendArgument(string[] args, ArgumentsBuilder argBuilder)
    {
        foreach (var arg in args)
        {
            argBuilder.Add(arg);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="paths">The paths to add to the archive</param>
    /// <param name="destination">The output path</param>
    /// <param name="compressor"></param>
    /// <param name="compressionLevel"></param>
    /// <param name="exclusions">Folders/items to exclude</param>
    /// <param name="cancellationToken"></param>
    /// <param name="workDirectory"></param>
    /// <returns></returns>
    public static IObservable<CommandEvent> Archive(
        string[] paths,
        string destination,
        string compressor,
        string compressionLevel,
        string[]? exclusions = null,
        CancellationToken? cancellationToken = null,
        string? workDirectory = null
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

        var cmd = Cli.Wrap(Os7ZPath).WithArguments(argBuilder => AppendArgument(args, argBuilder));
        if (workDirectory is not null)
        {
            cmd = cmd.WithWorkingDirectory(workDirectory);
        }

        return cancellationToken is not null ? cmd.Observe(cancellationToken.Value) : cmd.Observe();
    }

    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private static readonly string Os7ZPath = OperatingSystem.IsWindows()
        ? Path.Join(Dir, "resources", "7zip", "7z.exe")
        : "7zz";
    private static readonly string OsRarPath = OperatingSystem.IsMacOS() ? "unrar" : "unrar";

    [GeneratedRegex(@"(\d+(\.\d+)?)\s*%", RegexOptions.Compiled)]
    private static partial Regex ProgressRx();
}

public class SevenZipExtractException(string msg, Exception innerException)
    : Exception(msg, innerException);
