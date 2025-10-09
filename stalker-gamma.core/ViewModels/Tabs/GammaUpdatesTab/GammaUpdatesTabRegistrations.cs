using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Commands;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Queries;

namespace stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;

public static class GammaUpdatesTabRegistrations
{
    public static IServiceCollection RegisterGammaUpdatesTabServices(this IServiceCollection s) =>
        s.AddScoped<GetGitDiff.Handler>()
            .AddScoped<GetGitDiffFile.Handler>()
            .AddScoped<GitFetch.Handler>();
}
