using Microsoft.Extensions.DependencyInjection;
using stalker_gamma_gui.ViewModels.Tabs.GammaUpdatesTab.Queries;

namespace stalker_gamma_gui.ViewModels.Tabs.GammaUpdatesTab;

public static class GammaUpdatesTabRegistrations
{
    public static IServiceCollection RegisterGammaUpdatesTabServices(this IServiceCollection s) =>
        s.AddScoped<GetGitDiff.Handler>().AddScoped<GetGitDiffFile.Handler>();
}
