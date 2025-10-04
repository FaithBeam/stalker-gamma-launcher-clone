using System.Text;
using CliWrap;
using CliWrap.EventStream;

namespace stalker_gamma.core.Utilities;

public static class ArchiveUtility
{
    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public static async Task ExtractAsync(string archivePath, string destinationFolder)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        var cmd = Cli.Wrap(SevenZip)
            .WithArguments($"x \"{archivePath}\" -aoa -o\"{destinationFolder}\"")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr));
        var result = await cmd.ExecuteAsync();
        if (!result.IsSuccess)
        {
            throw new Exception($"{stdErr}\n{stdOut}");
        }
    }

    public static IObservable<CommandEvent> Extract(
        string archivePath,
        string destinationFolder,
        CancellationToken? ct = null,
        string? workingDirectory = null
    )
    {
        var cli = $"x " + $"-y " + $"\"{archivePath}\" " + $"-o\"{destinationFolder}\" ";
        var cmd = Cli.Wrap(SevenZip).WithArguments(cli);
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            cmd = cmd.WithWorkingDirectory(workingDirectory);
        }
        return ct is not null ? cmd.Observe(ct.Value) : cmd.Observe();
    }

    public static IObservable<CommandEvent> List(string archivePath, CancellationToken? ct = null)
    {
        var cli = $"l -slt {archivePath}";
        var cmd = Cli.Wrap(SevenZip).WithArguments(cli);
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
        var cli =
            $"a "
            + $"-bsp1 "
            + $"\"{destination}\" "
            + $"{string.Join(" ", paths.Select(x => $"\"{x}\""))} "
            + $"-m0={(compressor == "zstd" ? "bcj" : compressor)} "
            + $"{(compressor == "zstd" ? "-m1=zstd " : "")}"
            + $"-mx{compressionLevel} "
            + $"{(exclusions?.Length == 0 ? "" : string.Join(" ", exclusions!.Select(x => $"-xr!{x}")))}";
        var cmd = Cli.Wrap(SevenZip).WithArguments(cli);
        if (workDirectory is not null)
        {
            cmd = cmd.WithWorkingDirectory(workDirectory);
        }
        return cancellationToken is not null ? cmd.Observe(cancellationToken.Value) : cmd.Observe();
    }

    private const string Macos7Zip = "7zz";
    private const string Windows7Zip = "7z.exe";
    private const string Linux7Zip = "7zzs";
    private static readonly string SevenZipPath =
        OperatingSystem.IsWindows() ? Windows7Zip
        : OperatingSystem.IsMacOS() ? Macos7Zip
        : Linux7Zip;

    private static readonly string SevenZip = OperatingSystem.IsWindows()
        ? Path.Join(Dir, "Resources", "7zip", SevenZipPath)
        : SevenZipPath;
}
