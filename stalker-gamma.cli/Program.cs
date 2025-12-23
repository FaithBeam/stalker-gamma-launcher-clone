using System.Net;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using stalker_gamma.cli.Services;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Services.ModOrganizer;
using stalker_gamma.core.Services.ModOrganizer.DowngradeModOrganizer;
using stalker_gamma.core.Utilities;
using AnomalyInstaller = stalker_gamma.cli.Services.AnomalyInstaller;

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
                    .AddSingleton<Services.ProgressService>()
                    .AddScoped<DowngradeModOrganizer>()
                    .AddScoped<DisableNexusModHandlerLink>()
                    .AddScoped<WriteModOrganizerIni>()
                    .AddScoped<InstallModOrganizerGammaProfile>()
                    .AddScoped<GitUtility>()
                    .AddScoped<ModListRecordFactory>()
                    .AddScoped<ModDb>()
                    .AddScoped<AnomalyInstaller>()
                    .AddScoped<CustomGammaInstaller>()
                    .AddScoped<IOperatingSystemService, OperatingSystemService>()
                    .AddScoped<ICurlService, CurlService>()
                    .AddScoped<MirrorService>()
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
