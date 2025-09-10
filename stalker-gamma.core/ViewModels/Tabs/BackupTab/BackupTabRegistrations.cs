using Microsoft.Extensions.DependencyInjection;
using stalker_gamma.core.ViewModels.Tabs.BackupTab.Commands;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab;

public static class BackupTabRegistrations
{
    public static IServiceCollection RegisterBackupTabServices(this IServiceCollection s) =>
        s.AddSingleton<BackupTabProgressService>().AddScoped<RestoreBackup.Handler>();
}
