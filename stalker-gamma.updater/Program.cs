using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.MainWindow.Queries;

namespace stalker_gamma.updater;

public class Program
{
    public static async Task Main()
    {
        const string stalkerGammaProcessName = "stalker-gamma-gui.exe";

        var services = new ServiceCollection();
        services
            .AddHttpClient(
                "githubDlArchive",
                client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "stalker-gamma-clone/1.0");
                }
            )
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    AutomaticDecompression = DecompressionMethods.None,
                }
            );
        services
            .AddScoped<DownloadUpdate.Handler>()
            .AddScoped<IVersionService, VersionService>()
            .AddScoped<UpdateAvailable.Handler>();

        var sp = services.BuildServiceProvider();

        try
        {
            var procIdToKill = GetProcessIdByName(stalkerGammaProcessName);
            if (procIdToKill is > 0)
            {
                if (!KillProcessById(procIdToKill.Value))
                {
                    Environment.Exit(1);
                }
            }

            var updateAvailable = await sp.GetRequiredService<UpdateAvailable.Handler>()
                .ExecuteAsync();
            if (!updateAvailable.IsUpdateAvailable)
            {
                Console.WriteLine("No updates available");
                Environment.Exit(0);
            }

            var downloadUpdateHandler = sp.GetRequiredService<DownloadUpdate.Handler>();
            var cts = new CancellationTokenSource();
            var ct = cts.Token;
            var stream = await downloadUpdateHandler.ExecuteAsync(
                new DownloadUpdate.Command(updateAvailable.DownloadLink, ct)
            );

            await ZipFile.ExtractToDirectoryAsync(stream, ".", true, ct);

            await stream.DisposeAsync();

            Process.Start(
                new ProcessStartInfo { FileName = stalkerGammaProcessName, UseShellExecute = true }
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.ReadLine();
            throw;
        }
    }

    private static bool KillProcessById(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);

            if (process.HasExited)
            {
                return false;
            }
            process.Kill();
            process.WaitForExit();
            return true;
        }
        catch (ArgumentException)
        {
            // Process with the given ID doesn't exist
            return false;
        }
        catch (Exception ex)
        {
            // Handle other exceptions (e.g., access denied)
            Console.WriteLine($"Error killing process: {ex.Message}");
            return false;
        }
    }

    private static int? GetProcessIdByName(string processName)
    {
        try
        {
            // Remove .exe extension if present
            processName = processName.Replace(".exe", "");

            var processes = Process.GetProcessesByName(processName);

            if (processes.Length > 0)
            {
                // Return the first matching process ID
                return processes[0].Id;
            }

            return null; // No process found
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting process by name: {ex.Message}");
            return null;
        }
    }
}

public class VersionService : IVersionService
{
    public string GetVersion()
    {
        var versionInfo = FileVersionInfo.GetVersionInfo("stalker-gamma-gui.exe");
        return versionInfo.FileVersion!;
    }
}
