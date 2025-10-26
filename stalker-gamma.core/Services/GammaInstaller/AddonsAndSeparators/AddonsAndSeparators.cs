using System.Linq.Expressions;
using System.Threading.Channels;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;

namespace stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators;

public class AddonsAndSeparators(
    ProgressService progressService,
    ModListRecordFactory modListRecordFactory
)
{
    private readonly ModListRecordFactory _modListRecordFactory = modListRecordFactory;

    public async Task Install(
        string downloadsPath,
        string modsPaths,
        string modListFile,
        bool forceGitDownload,
        bool checkMd5,
        bool updateLargeFiles,
        bool forceZipExtraction,
        bool useCurlImpersonate
    )
    {
        progressService.UpdateProgress(
            """

            ==================================================================================
                                    Add-ons and Separators Installation                       
            ==================================================================================

            """
        );

        if (!Directory.Exists(downloadsPath))
        {
            Directory.CreateDirectory(downloadsPath);
        }

        if (!Directory.Exists(modsPaths))
        {
            Directory.CreateDirectory(modsPaths);
        }

        var files = (await File.ReadAllLinesAsync(modListFile))
            .Select(x => _modListRecordFactory.Create(x))
            .ToList();

        var total = files.Count;

        var counter = 0;

        var summedFiles = files.Select(f => new { Count = ++counter, File = f }).ToList();

        var separators = summedFiles
            .Where(f => f.File is Separator)
            .Select(f => new { f.Count, File = f.File as Separator })
            .Select(f => new
            {
                f.Count,
                f.File,
                Action = (Action)(
                    () =>
                    {
                        progressService.UpdateProgress(
                            $"""
                            _______________ {f.File?.Name} separator _______________
                            Creating MO2 separator in {Path.Join(modsPaths, f.File?.FolderName)}
                            """
                        );
                        f.File?.WriteMetaIni(modsPaths, f.Count);
                        progressService.UpdateProgress(" ");
                    }
                ),
            });
        var downloadableRecords = summedFiles
            .Where(f => f.File is DownloadableRecord)
            .Select(f => new { f.Count, File = f.File as DownloadableRecord })
            .Select(f => new
            {
                f.Count,
                f.File,
                Dl = (Func<Task<bool>>)(
                    async () =>
                    {
                        if (
                            !await f.File!.ShouldDownloadAsync(
                                downloadsPath,
                                checkMd5,
                                forceGitDownload
                            )
                        )
                        {
                            return false;
                        }

                        progressService.UpdateProgress(
                            $"_______________ {f.File.AddonName} _______________"
                        );

                        if (
                            !await f.File.DownloadAsync(downloadsPath, useCurlImpersonate)
                            || !await f.File.ShouldDownloadAsync(
                                downloadsPath,
                                checkMd5,
                                forceGitDownload
                            )
                        )
                        {
                            return true;
                        }

                        progressService.UpdateProgress(
                            $"Md5 mismatch in downloaded file: {f.File.DlPath}. Downloading again."
                        );
                        await f.File.DownloadAsync(downloadsPath, useCurlImpersonate);

                        return true;
                    }
                ),
                Extract = (Func<Task>)(
                    async () => await ExtractAsync(f.File!, modsPaths, total, f.Count)
                ),
            });

        foreach (var separator in separators)
        {
            separator.Action();
        }

        // download
        var dlChannel = Channel.CreateUnbounded<(Func<Task> extractAction, bool justDownloaded)>();
        var t1 = Task.Run(async () =>
        {
            foreach (var dlRec in downloadableRecords)
            {
                var extract = await dlRec.Dl();
                await dlChannel.Writer.WriteAsync((dlRec.Extract, extract));
            }

            dlChannel.Writer.TryComplete();
        });

        // extract
        var t2 = Task.Run(async () =>
        {
            await foreach (var item in dlChannel.Reader.ReadAllAsync())
            {
                var (extractAction, justDownloaded) = item;
                if (forceZipExtraction || justDownloaded)
                {
                    await extractAction.Invoke();
                }
            }
        });

        await Task.WhenAll(t1, t2);
    }

    private async Task ExtractAsync(
        DownloadableRecord downloadableRecord,
        string modsPaths,
        int total,
        int counter
    )
    {
        var extractPath = Path.Join(
            modsPaths,
            $"{counter}-{downloadableRecord.AddonName}{downloadableRecord.Patch}"
        );

        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(extractPath);
        }

        downloadableRecord.CleanExtractPath(extractPath);

        await downloadableRecord.WriteMetaIniAsync(extractPath);

        progressService.UpdateProgress($"\tExtracting to {extractPath}");
        await downloadableRecord.ExtractAsync(extractPath);
        progressService.UpdateProgress(counter / (double)total * 100);
    }
}
