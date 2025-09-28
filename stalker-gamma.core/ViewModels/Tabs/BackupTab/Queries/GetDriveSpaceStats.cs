using stalker_gamma.core.ViewModels.Tabs.BackupTab.Models;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Queries;

public static class GetDriveSpaceStats
{
    public sealed record Query(string GammaFolder);

    public sealed class Handler
    {
        public DriveSpaceStats Execute(Query q)
        {
            var pathRoot = Path.GetPathRoot(q.GammaFolder);
            DriveInfo drive = new(pathRoot!);
            var backupSize = Directory
                .GetFiles(q.GammaFolder, "*.*", SearchOption.AllDirectories)
                .Sum(x => new FileInfo(x).Length);
            return new DriveSpaceStats(
                drive.TotalSize,
                drive.TotalSize - drive.TotalFreeSpace,
                backupSize
            );
        }
    }
}
