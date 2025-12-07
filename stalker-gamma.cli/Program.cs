using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.cli.Services;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using AnomalyInstaller = stalker_gamma.cli.Services.AnomalyInstaller;

namespace stalker_gamma.cli;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var app = ConsoleApp
            .Create()
            .ConfigureServices(services =>
                services
                    .AddScoped<ModDb>()
                    .AddScoped<AnomalyInstaller>()
                    .AddScoped<CustomGammaInstaller>()
                    .AddScoped<IOperatingSystemService, OperatingSystemService>()
                    .AddScoped<ICurlService, CurlService>()
                    .AddScoped<MirrorService>()
                    .AddHttpClient()
            );

        await app.RunAsync(args);
    }
}
