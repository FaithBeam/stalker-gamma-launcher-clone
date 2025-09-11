namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Queries;

public static class GetGammaBackupFolder
{
    public sealed class Handler
    {
        public string Execute()
        {
            return Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GAMMA"
            );
        }
    }
}
