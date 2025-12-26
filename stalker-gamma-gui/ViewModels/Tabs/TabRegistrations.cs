using Microsoft.Extensions.DependencyInjection;
using stalker_gamma_gui.ViewModels.Tabs.MainTab.Queries;
using stalker_gamma_gui.ViewModels.Tabs.Queries;

namespace stalker_gamma_gui.ViewModels.Tabs;

public static class TabRegistrations
{
    public static IServiceCollection RegisterCommonTabServices(this IServiceCollection s) =>
        s.AddScoped<GetStalkerGammaLastCommit.Handler>()
            .AddScoped<GetGitHubRepoCommits.Handler>()
            .AddScoped<GetAnomalyPath.Handler>()
            .AddScoped<GetGammaPath.Handler>()
            .AddScoped<GetGammaBackupFolder.Handler>();
}
