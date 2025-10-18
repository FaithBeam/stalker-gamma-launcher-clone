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

        foreach (var file in files)
        {
            counter++;

            if (file is Separator separator)
            {
                progressService.UpdateProgress(
                    $"""
                    _______________ {separator.Name} separator _______________
                    Creating MO2 separator in {Path.Join(modsPaths, separator.FolderName)}
                    """
                );
                separator.WriteMetaIni(modsPaths, counter);
                progressService.UpdateProgress(" ");
                continue;
            }

            var downloadableRecord = (DownloadableRecord)file;
            var extract = false;

            if (
                downloadableRecord
                    .ShouldDownloadAsync(downloadsPath, checkMd5, forceGitDownload)
                    .GetAwaiter()
                    .GetResult()
            )
            {
                progressService.UpdateProgress(
                    $"_______________ {downloadableRecord.AddonName} _______________"
                );
                if (
                    downloadableRecord
                        .DownloadAsync(downloadsPath, useCurlImpersonate)
                        .GetAwaiter()
                        .GetResult()
                    && downloadableRecord
                        .ShouldDownloadAsync(downloadsPath, checkMd5, forceGitDownload)
                        .GetAwaiter()
                        .GetResult()
                )
                {
                    progressService.UpdateProgress(
                        $"Md5 mismatch in downloaded file: {downloadableRecord.DlPath}. Downloading again."
                    );
                    downloadableRecord
                        .DownloadAsync(downloadsPath, useCurlImpersonate)
                        .GetAwaiter()
                        .GetResult();
                }
                extract = true;
            }

            if (forceZipExtraction || extract)
            {
                Extract(downloadableRecord, modsPaths, total, counter);
            }
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
