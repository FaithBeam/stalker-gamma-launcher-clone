using Microsoft.Extensions.DependencyInjection;

namespace stalker_gamma.core.ViewModels.MainWindow;

public static class MainWindowRegistrations
{
    public static IServiceCollection RegisterMainWindowServices(this IServiceCollection s) =>
        s.AddScoped<SwitchToMyFork.SwitchToMyFork.Handler>();
}
