using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.Utilities;

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
                    .AddScoped<IOperatingSystemService, OperatingSystemService>()
                    .AddScoped<ICurlService, CurlService>()
                    .AddScoped<MirrorService>()
                    .AddHttpClient()
            );

        await app.RunAsync(args);
    }
}
