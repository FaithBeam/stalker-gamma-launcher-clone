using ConsoleAppFramework;

namespace stalker_gamma.cli;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var app = ConsoleApp.Create();

        await app.RunAsync(args);
    }
}
