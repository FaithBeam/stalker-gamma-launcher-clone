using Microsoft.Extensions.DependencyInjection;

namespace stalker_gamma_gui.ViewModels.Dialogs.DownloadProgress;

public static class DownloadProgressRegistrations
{
    // public static IServiceCollection RegisterDownloadProgressServices(this IServiceCollection s) =>
    //     s.AddScoped<DownloadUpdate.Handler>();

    public static IServiceCollection RegisterDownloadProgressServices(this IServiceCollection s) =>
        s;
}
