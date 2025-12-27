using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using stalker_gamma_gui.ViewModels.Tabs.BackupTab.Services;
using stalker_gamma.core.Utilities;

namespace stalker_gamma_gui.ViewModels.Tabs.BackupTab.Commands;

public static class RestoreBackup
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="PathToArchive">Path to .7z file</param>
    /// <param name="DirectoryToExtractTo">Path to GAMMA RC3 folder</param>
    public sealed record Command(
        string PathToArchive,
        string DirectoryToExtractTo,
        string WorkingDirectory,
        string[] DirsToClean
    );

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

            foreach (var dir in c.DirsToClean)
            {
                progress.UpdateProgress($"Deleting directory: {dir}");
                new DirectoryInfo(dir)
                    .GetDirectories("*", SearchOption.AllDirectories)
                    .ToList()
                    .ForEach(di =>
                    {
                        di.Attributes &= ~FileAttributes.ReadOnly;
                        di.GetFiles("*", SearchOption.TopDirectoryOnly)
                            .ToList()
                            .ForEach(fi => fi.IsReadOnly = false);
                    });
                Directory.Delete(dir, true);
            }

            await SevenZipUtility.ExtractAsync(
                c.PathToArchive,
                c.DirectoryToExtractTo,
                workingDirectory: c.WorkingDirectory,
                onProgress: progress.UpdateProgress
            );
        }
    }
}

public class RestoreBackupException(string msg) : Exception(msg);
