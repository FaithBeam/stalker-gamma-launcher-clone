namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Commands;

public static class DeleteBackup
{
    public sealed record Command(string BackupModPath, string BackupName);

    public sealed class Handler
    {
        public void Execute(Command c)
        {
            var joined = Path.Join(c.BackupModPath, c.BackupName);
            if (File.Exists(joined))
            {
                File.Delete(joined);
            }
        }
    }
}
