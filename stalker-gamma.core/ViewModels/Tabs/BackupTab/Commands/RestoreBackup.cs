using System.Reactive.Linq;
using CliWrap.EventStream;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Commands;

public static class RestoreBackup
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="PathToArchive">Path to .7z file</param>
    /// <param name="DirectoryToExtractTo">Path to GAMMA RC3 folder</param>
    public sealed record Command(string PathToArchive, string DirectoryToExtractTo);

    public sealed class Handler(BackupTabProgressService progress)
    {
        public async Task ExecuteAsync(Command c)
        {
            if (!Directory.Exists(c.DirectoryToExtractTo))
            {
                throw new RestoreBackupException(
                    $"Directory to extract to doesn't exist {c.DirectoryToExtractTo}"
                );
            }
            if (!File.Exists(c.PathToArchive))
            {
                throw new RestoreBackupException($"Backup archive doesn't exist {c.PathToArchive}");
            }

            var dirToExtractToWithMods = Path.Join(c.DirectoryToExtractTo, "mods");
            if (Directory.Exists(dirToExtractToWithMods))
            {
                progress.UpdateProgress(
                    $"Deleting current mods directory: {dirToExtractToWithMods}"
                );
                new DirectoryInfo(dirToExtractToWithMods)
                    .GetDirectories("*", SearchOption.AllDirectories)
                    .ToList()
                    .ForEach(di =>
                    {
                        di.Attributes &= ~FileAttributes.ReadOnly;
                        di.GetFiles("*", SearchOption.TopDirectoryOnly)
                            .ToList()
                            .ForEach(fi => fi.IsReadOnly = false);
                    });
                Directory.Delete(dirToExtractToWithMods, true);
            }

            await Utilities
                .ArchiveUtility.Extract(c.PathToArchive, c.DirectoryToExtractTo)
                .ForEachAsync(cmdEvt =>
                {
                    switch (cmdEvt)
                    {
                        case StandardErrorCommandEvent stdErr:
                            progress.UpdateProgress(stdErr.Text);
                            break;
                        case StandardOutputCommandEvent stdOut:
                            progress.UpdateProgress(stdOut.Text);
                            break;
                    }
                });
        }
    }
}

public class RestoreBackupException(string msg) : Exception(msg);
