using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab;

public static class MainTabRegistrations
{
    public static IServiceCollection RegisterMainTabServices(this IServiceCollection services) =>
        services
            .AddScoped<DiffMods.Handler>()
            .AddScoped<GetStalkerGammaLastCommit.Handler>()
            .AddScoped<GetGitHubRepoCommits.Handler>();
}
