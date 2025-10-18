using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;
using stalker_gamma.core.ViewModels.Tabs.Queries;

namespace stalker_gamma.core.ViewModels.Tabs;

public static class TabRegistrations
{
    public static IServiceCollection RegisterCommonTabServices(this IServiceCollection s) =>
        s.AddScoped<GetStalkerGammaLastCommit.Handler>()
            .AddScoped<GetGitHubRepoCommits.Handler>()
            .AddScoped<GetAnomalyPath.Handler>()
            .AddScoped<GetGammaPath.Handler>()
            .AddScoped<GetGammaBackupFolder.Handler>();
}
