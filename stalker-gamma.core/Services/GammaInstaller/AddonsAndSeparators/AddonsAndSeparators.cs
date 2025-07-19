using System.Threading.Channels;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators;

public partial class AddonsAndSeparators(ProgressService progressService, ModDb modDb)
{
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
            .Select(x => ParseModListRecord.ParseLine(x, modDb))
            .ToList();

        var total = files.Count;

        var counter = 0;

        var extractChannel = Channel.CreateUnbounded<(DownloadableRecord, string, int, int)>();
        var reader = Task.Run(() => ExtractChannelReader(extractChannel.Reader));
        var writer = Task.Run(async () =>
        {
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
                    await downloadableRecord.ShouldDownloadAsync(
                        downloadsPath,
                        checkMd5,
                        forceGitDownload
                    )
                )
                {
                    progressService.UpdateProgress(
                        $"_______________ {downloadableRecord.AddonName} _______________"
                    );
                    await downloadableRecord.DownloadAsync(downloadsPath, useCurlImpersonate);
                    if (await downloadableRecord.ShouldDownloadAsync(downloadsPath, checkMd5, forceGitDownload))
                    {
                        progressService.UpdateProgress($"Md5 mismatch in downloaded file: {downloadableRecord.DlPath}. Downloading again.");
                        await downloadableRecord.DownloadAsync(downloadsPath, useCurlImpersonate);
                    }
                    extract = true;
                }

                if (forceZipExtraction || extract)
                {
                    extractChannel.Writer.TryWrite((downloadableRecord, modsPaths, total, counter));
                }
            }
            extractChannel.Writer.Complete();
        });

        await Task.WhenAll(writer, reader);
        await extractChannel.Reader.Completion;
    }

    private async Task ExtractChannelReader(
        ChannelReader<(DownloadableRecord, string, int, int)> reader
    )
    {
        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var vars))
            {
                var downloadableRecord = vars.Item1;
                var modsPaths = vars.Item2;
                var total = vars.Item3;
                var counter = vars.Item4;

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
    }
}
