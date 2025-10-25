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
                        f.File?.WriteMetaIni(modsPaths, counter);
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
                Dl = (Func<bool>)(
                    () =>
                    {
                        if (
                            !f.File!.ShouldDownloadAsync(downloadsPath, checkMd5, forceGitDownload)
                                .GetAwaiter()
                                .GetResult()
                        )
                        {
                            return false;
                        }

                        progressService.UpdateProgress(
                            $"_______________ {f.File.AddonName} _______________"
                        );
                        
                        if (
                            !f
                                .File.DownloadAsync(downloadsPath, useCurlImpersonate)
                                .GetAwaiter()
                                .GetResult()
                            || !f
                                .File.ShouldDownloadAsync(downloadsPath, checkMd5, forceGitDownload)
                                .GetAwaiter()
                                .GetResult()
                        )
                        {
                            return true;
                        }
                        
                        progressService.UpdateProgress(
                            $"Md5 mismatch in downloaded file: {f.File.DlPath}. Downloading again."
                        );
                        f.File.DownloadAsync(downloadsPath, useCurlImpersonate)
                            .GetAwaiter()
                            .GetResult();

                        return true;
                    }
                ),
                Extract = (Action)(
                    () =>
                    {
                        if (forceZipExtraction)
                        {
                            Extract(f.File!, modsPaths, total, counter);
                        }
                    }
                ),
            });

        foreach (var separator in separators)
        {
            separator.Action();
        }

        // download
        var dlChannel = Channel.CreateUnbounded<Action>();
        var groupedDls = downloadableRecords.GroupBy(x => x.File!.DlLink);
        _ = Task.Run(async () =>
        {
            foreach (var groupedDl in groupedDls)
            {
                groupedDl.First().Dl();

                foreach (var inner in groupedDl)
                {
                    await dlChannel.Writer.WriteAsync(inner.Extract);
                }
            }
            
            dlChannel.Writer.Complete();
        });

        // extract
        await foreach (var item in dlChannel.Reader.ReadAllAsync())
        {
            item.Invoke();
        }
    }

    private void Extract(
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

        downloadableRecord.WriteMetaIniAsync(extractPath).GetAwaiter().GetResult();

        progressService.UpdateProgress($"\tExtracting to {extractPath}");
        downloadableRecord.ExtractAsync(extractPath).GetAwaiter().GetResult();
        progressService.UpdateProgress(counter / (double)total * 100);
    }
}
