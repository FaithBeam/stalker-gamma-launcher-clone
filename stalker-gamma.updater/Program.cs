using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.updater;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 4)
        {
            Environment.Exit(1);
        }

        var procIdToKill = int.Parse(args[0]);
        var dlLink = args[1];
        var dlPath = args[2];
        var extractPath = args[3];

        if (
            string.IsNullOrWhiteSpace(dlLink)
            || string.IsNullOrWhiteSpace(dlPath)
            || string.IsNullOrWhiteSpace(extractPath)
        )
        {
            Environment.Exit(1);
        }

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
        services.AddScoped<DownloadUpdate.Handler>();

        var sp = services.BuildServiceProvider();

        var downloadUpdateHandler = sp.GetRequiredService<DownloadUpdate.Handler>();
        var progress = new Progress<double>();
        var cts = new CancellationTokenSource();
        var ct = cts.Token;
        await downloadUpdateHandler.ExecuteAsync(
            new DownloadUpdate.Command(dlLink, "stalker-gamma.zip", progress, ct)
        );

        if (!KillProcessById(procIdToKill))
        {
            Environment.Exit(1);
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
}
