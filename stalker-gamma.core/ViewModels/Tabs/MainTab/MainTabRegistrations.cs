using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Commands;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab;

public static class MainTabRegistrations
{
    public static IServiceCollection RegisterMainTabServices(this IServiceCollection services) =>
        services
            .AddScoped<DiffMods.Handler>()
            .AddScoped<IIsMo2VersionDowngraded, IsMo2VersionDowngraded.Handler>()
            .AddScoped<IUserLtxSetToFullscreenWine, UserLtxSetToFullscreenWine.Handler>()
            .AddScoped<IIsMo2Initialized, IsMo2Initialized.Handler>()
            .AddScoped<IGetLocalGammaVersion, GetLocalGammaVersion.Handler>()
            .AddScoped<
                IUserLtxReplaceFullscreenWithBorderlessFullscreen,
                UserLtxReplaceFullscreenWithBorderlessFullscreen.Handler
            >()
            .AddScoped<AddFoldersToWinDefenderExclusion.Handler>()
            .AddScoped<EnableLongPathsOnWindows.Handler>();
}
