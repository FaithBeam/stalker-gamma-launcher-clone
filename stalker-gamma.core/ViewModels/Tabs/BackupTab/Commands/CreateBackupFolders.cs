namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Commands;

public static class CreateBackupFolders
{
    public sealed record Command(
        string GammaBackupFolder,
        string ModsBackupPath,
        string FullBackupPath
    );

    public sealed class Handler
    {
        public void Execute(Command c)
        {
            if (!Directory.Exists(c.GammaBackupFolder))
            {
                Directory.CreateDirectory(c.GammaBackupFolder);
            }

            if (!Directory.Exists(c.ModsBackupPath))
            {
                Directory.CreateDirectory(c.ModsBackupPath);
            }

            if (!Directory.Exists(c.FullBackupPath))
            {
                Directory.CreateDirectory(c.FullBackupPath);
            }
        }
    }
}
