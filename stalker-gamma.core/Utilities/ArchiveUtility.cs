using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.EventStream;
using CliWrap.Exceptions;

namespace stalker_gamma.core.Utilities;

public static partial class ArchiveUtility
{
    public static async Task ExtractAsync(string archivePath, string destinationFolder)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        var args = PartJoin(
            Os7ZPath,
            "x",
            $"\"{archivePath}\"",
            "-aoa",
            $"-o\"{destinationFolder}\""
        );
        var cmd = Cli.Wrap(OsShell)
            .WithArguments(argBuilder => argBuilder.Add(OsShellArgs).Add(args))
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
        var args = PartJoin(
            Os7ZPath,
            "x",
            "-y",
            $"\"{archivePath}\"",
            $"-o\"{destinationFolder}\""
        );
        var cmd = Cli.Wrap(OsShell)
            .WithArguments(argBuilder => argBuilder.Add(OsShellArgs).Add(args));
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
        var args = PartJoin(
            Os7ZPath,
            "x",
            "-y",
            "-bsp1",
            $"\"{archivePath}\"",
            $"-o\"{destinationFolder}\""
        );
        var cmd = Cli.Wrap(OsShell)
            .WithArguments(argBuilder => argBuilder.Add(OsShellArgs).Add(args));
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
        var cli = PartJoin(Os7ZPath, "l", "-slt", archivePath);
        var cmd = Cli.Wrap(OsShell)
            .WithArguments(argBuilder => argBuilder.Add(OsShellArgs).Add(cli));
        return ct is not null ? cmd.Observe(ct.Value) : cmd.Observe();
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
        var cli = PartJoin(
            Os7ZPath,
            "a",
            "-bsp",
            $"\"{destination}\"",
            $"{string.Join(" ", paths.Select(x => $"\"{x}\""))}",
            $"-m0={(compressor == "zstd" ? "bcj" : compressor)}",
            $"{(compressor == "zstd" ? "-m1=zstd " : "")}",
            $"-mx{compressionLevel}",
            $"{(exclusions?.Length == 0 ? "" : string.Join(" ", exclusions!.Select(x => $"-xr!{x}")))}"
        );

        var cmd = Cli.Wrap(OsShell)
            .WithArguments(argBuilder => argBuilder.Add(OsShellArgs).Add(cli));
        if (workDirectory is not null)
        {
            cmd = cmd.WithWorkingDirectory(workDirectory);
        }

        return cancellationToken is not null ? cmd.Observe(cancellationToken.Value) : cmd.Observe();
    }

    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private static readonly string OsShell = OperatingSystem.IsWindows() ? "cmd.exe" : "bash";
    private static readonly string OsShellArgs = OperatingSystem.IsWindows() ? "/C" : "-c";
    private static readonly string Os7ZPath = OperatingSystem.IsWindows()
        ? Path.Join(Dir, "resources", "7zip", "7z.exe")
        : "7z";

    private static string PartJoin(params string[] parts) =>
        string.Join(
            ' ',
            parts.Select(p =>
                $"{(OperatingSystem.IsWindows() ? "" : "")}{p}{(OperatingSystem.IsWindows() ? "" : "")}"
            )
        );

    [GeneratedRegex(@"(\d+(\.\d+)?)\s*%", RegexOptions.Compiled)]
    private static partial Regex ProgressRx();
}

public class SevenZipExtractException(string msg, Exception innerException)
    : Exception(msg, innerException);
