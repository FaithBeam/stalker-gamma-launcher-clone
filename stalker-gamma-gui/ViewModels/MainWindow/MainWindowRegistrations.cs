using Microsoft.Extensions.DependencyInjection;
using stalker_gamma_gui.ViewModels.MainWindow.Factories;
using stalker_gamma.core.Services;

namespace stalker_gamma_gui.ViewModels.MainWindow;

public static class MainWindowRegistrations
{
    public static IServiceCollection RegisterMainWindowServices(this IServiceCollection s) =>
        s.AddScoped<UpdateLauncherDialogVmFactory>().AddScoped<UpdateAvailable.Handler>();
}
