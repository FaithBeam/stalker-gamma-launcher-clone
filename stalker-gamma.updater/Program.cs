using System.Net;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.MainWindow.Queries;

namespace stalker_gamma.updater;

public class Program
{
    public static async Task Main(string[] args)
    {
        var app = ConsoleApp
            .Create()
            .ConfigureServices(services =>
                services
                    .AddScoped<DownloadUpdate.Handler>()
                    .AddScoped<IVersionService, updater.Commands.VersionService>()
                    .AddScoped<UpdateAvailable.Handler>()
                    .AddHttpClient(
                        "githubDlArchive",
                        client =>
                        {
                            client.DefaultRequestHeaders.Add(
                                "User-Agent",
                                "stalker-gamma-clone/1.0"
                            );
                        }
                    )
                    .ConfigurePrimaryHttpMessageHandler(() =>
                        new SocketsHttpHandler
                        {
                            EnableMultipleHttp2Connections = true,
                            AutomaticDecompression = DecompressionMethods.None,
                        }
                    )
            );

        await app.RunAsync(args);
    }
}
