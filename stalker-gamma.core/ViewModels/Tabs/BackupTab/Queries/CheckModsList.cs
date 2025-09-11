using DynamicData;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Queries;

public static class CheckModsList
{
    public sealed record Query(SourceList<string> BackupsSrcList, string ModsBackupPath);

    public sealed class Handler
    {
        public void Execute(Query q)
        {
            q.BackupsSrcList.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(Directory.GetFiles(q.ModsBackupPath));
            });
        }
    }
}
