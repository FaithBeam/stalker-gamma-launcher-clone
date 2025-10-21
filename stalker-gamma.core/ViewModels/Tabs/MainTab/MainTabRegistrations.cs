using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Commands;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab;

public static class MainTabRegistrations
{
    public static IServiceCollection RegisterMainTabServices(this IServiceCollection services) =>
        services
            .AddScoped<DiffMods.Handler>()
            .AddScoped<AddFoldersToWinDefenderExclusion.Handler>()
            .AddScoped<EnableLongPathsOnWindows.Handler>();
}
