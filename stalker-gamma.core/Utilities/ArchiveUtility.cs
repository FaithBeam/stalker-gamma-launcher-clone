using System.Text;
using CliWrap;

namespace stalker_gamma.core.Utilities;

public static class ArchiveUtility
{
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

    private const string Macos7Zip = "7zz";
    private const string Windows7Zip = "7z.exe";
    private const string Linux7Zip = "7zzs";
    private static readonly string SevenZipPath =
        OperatingSystem.IsWindows() ? Windows7Zip
        : OperatingSystem.IsMacOS() ? Macos7Zip
        : Linux7Zip;

    private static readonly string SevenZip = OperatingSystem.IsWindows()
        ? Path.Join("Resources", "7zip", SevenZipPath)
        : SevenZipPath;
}
