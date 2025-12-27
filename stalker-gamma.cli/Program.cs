using System.Net;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using stalker_gamma.cli.Services;
using stalker_gamma.core.Factories;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstallerServices;
using stalker_gamma.core.Services.ModOrganizer;
using stalker_gamma.core.Services.ModOrganizer.DownloadModOrganizer;

namespace stalker_gamma.cli;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var log = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        var app = ConsoleApp
            .Create()
            .ConfigureServices(services =>
                services
                    .AddSingleton<ILogger>(log)
                    .AddSingleton<GlobalSettings>()
                    .AddSingleton<ProgressThrottleService>()
                    .AddSingleton<ProgressService>()
                    .AddScoped<GetModListService>()
                    .AddScoped<DownloadAddonUtility>()
                    .AddScoped<DownloadAndInstallCustomModlist>()
                    .AddScoped<DownloadGitHubArchive>()
                    .AddScoped<EnrichGammaInstaller>()
                    .AddScoped<EnrichDownloadAndExtractGitRepoFactory>()
                    .AddScoped<EnrichAnomalyInstaller>()
                    .AddScoped<EnableLongPathsOnWindowsService>()
                    .AddScoped<AddFoldersToWinDefenderExclusionService>()
                    .AddScoped<DownloadModOrganizerService>()
                    .AddScoped<DisableNexusModHandlerLink>()
                    .AddScoped<WriteModOrganizerIniService>()
                    .AddScoped<InstallModOrganizerGammaProfile>()
                    .AddScoped<AnomalyInstaller>()
                    .AddScoped<GammaInstaller>()
                    .AddHttpClient()
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
