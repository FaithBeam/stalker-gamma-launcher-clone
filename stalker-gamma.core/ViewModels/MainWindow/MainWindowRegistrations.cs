using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.ViewModels.MainWindow.Factories;
using stalker_gamma.core.ViewModels.MainWindow.Queries;

namespace stalker_gamma.core.ViewModels.MainWindow;

public static class MainWindowRegistrations
{
    public static IServiceCollection RegisterMainWindowServices(this IServiceCollection s) =>
        s.AddScoped<UpdateLauncherDialogVmFactory>().AddScoped<UpdateAvailable.Handler>();
}
