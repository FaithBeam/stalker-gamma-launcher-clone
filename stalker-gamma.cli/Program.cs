using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using stalker_gamma.core.Services;
using Stalker.Gamma.Extensions;

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
                    .AddScoped<EnableLongPathsOnWindowsService>()
                    .AddScoped<AddFoldersToWinDefenderExclusionService>()
                    .RegisterCoreGammaServices()
            );

        await app.RunAsync(args);
    }
}
