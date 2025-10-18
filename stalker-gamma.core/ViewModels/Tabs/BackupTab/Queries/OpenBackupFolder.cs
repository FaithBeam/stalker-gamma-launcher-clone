using System.Diagnostics;
using System.Runtime.InteropServices;
using stalker_gamma.core.ViewModels.Tabs.Queries;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Queries;

public static class OpenBackupFolder
{
    public sealed class Handler(GetGammaBackupFolder.Handler getGammaBackupFolderHandler)
    {
        public void Execute()
        {
            var psi = new ProcessStartInfo
            {
                Arguments = getGammaBackupFolderHandler.Execute(),
                UseShellExecute = true,
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi.FileName = "explorer.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                psi.FileName = "xdg-open";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                psi.FileName = "open";
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            Process.Start(psi);
        }
    }
}
