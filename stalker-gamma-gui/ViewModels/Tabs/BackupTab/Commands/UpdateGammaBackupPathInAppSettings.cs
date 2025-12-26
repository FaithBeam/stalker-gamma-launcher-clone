using System.Threading.Tasks;
using stalker_gamma.core.Models;

namespace stalker_gamma_gui.ViewModels.Tabs.BackupTab.Commands;

public static class UpdateGammaBackupPathInAppSettings
{
    public sealed record Command(string NewPath);

    public sealed class Handler(GlobalSettings gs)
    {
        public async Task ExecuteAsync(Command c)
        {
            gs.GammaBackupPath = c.NewPath;
            await gs.WriteAppSettingsAsync();
        }
    }
}
