using Microsoft.Extensions.DependencyInjection;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab;

public static class BackupTabRegistrations
{
    public static IServiceCollection RegisterBackupTabServices(this IServiceCollection s) =>
        s.AddSingleton<BackupTabProgressService>()
            .AddScoped<Commands.RestoreBackup.Handler>()
            .AddScoped<Commands.DeleteBackup.Handler>()
            .AddScoped<Commands.CreateBackupFolders.Handler>()
            .AddScoped<Commands.CreateBackup.Handler>()
            .AddScoped<Queries.GetEstimate.Handler>()
            .AddScoped<Queries.GetAnomalyPath.Handler>()
            .AddScoped<Queries.GetGammaPath.Handler>()
            .AddScoped<Queries.GetDriveSpaceStats.Handler>()
            .AddScoped<Queries.CheckModsList.Handler>()
            .AddScoped<Queries.GetGammaBackupFolder.Handler>();
}
