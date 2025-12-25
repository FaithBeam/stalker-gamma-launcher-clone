using System.Diagnostics;
using System.IO.Compression;
using ConsoleAppFramework;
using stalker_gamma.core.Services;

namespace stalker_gamma.updater.Commands;

[RegisterCommands]
public class DownloadUpdate(
    UpdateAvailable.Handler updateAvailableHandler,
    updater.DownloadUpdate.Handler downloadUpdateHandler
)
{
    /// <summary>
    /// Downloads and extracts the latest available update to the specified destination directory.
    /// If no destination directory is provided, a temporary directory will be used.
    /// </summary>
    /// <param name="destinationDirectory">The directory where the update will be downloaded and extracted. If null or empty, a default temporary directory will be used.</param>
    /// <returns>An integer representing the execution status. Returns 0 if the operation completes successfully.</returns>
    public async Task<int> Download(string? destinationDirectory = null)
    {
        try
        {
            destinationDirectory = string.IsNullOrWhiteSpace(destinationDirectory)
                ? Path.Join(Path.GetTempPath(), "stalker-gamma-updater")
                : destinationDirectory;

            var updateAvailable = await _updateAvailableHandler.ExecuteAsync();
            if (!updateAvailable.IsUpdateAvailable)
            {
                Console.WriteLine("No updates available");
                Environment.Exit(0);
            }

            if (Directory.Exists(destinationDirectory))
            {
                Directory.Delete(destinationDirectory, true);
            }
            Directory.CreateDirectory(destinationDirectory);

            var cts = new CancellationTokenSource();
            var ct = cts.Token;

            var stream = await _downloadUpdateHandler.ExecuteAsync(
                new updater.DownloadUpdate.Command(updateAvailable.DownloadLink, ct)
            );

            await ZipFile.ExtractToDirectoryAsync(stream, destinationDirectory, true, ct);

            await stream.DisposeAsync();

            return 0;
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"{e}");
            return 1;
        }
    }

    private readonly UpdateAvailable.Handler _updateAvailableHandler = updateAvailableHandler;
    private readonly updater.DownloadUpdate.Handler _downloadUpdateHandler = downloadUpdateHandler;
}

public class VersionService : IVersionService
{
    public string GetVersion()
    {
        var versionInfo = FileVersionInfo.GetVersionInfo("stalker-gamma-gui.exe");
        return versionInfo.FileVersion!;
    }
}
