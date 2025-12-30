using Stalker.Gamma.GammaInstallerServices;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.Models;

public class ModDbRecord(
    GammaProgress gammaProgress,
    string name,
    string url,
    string niceUrl,
    string archiveName,
    string? md5,
    string gammaDir,
    string outputDirName,
    IList<string> instructions,
    ArchiveUtility archiveUtility,
    ModDbUtility modDbUtility
) : IDownloadableRecord
{
    public string Name { get; } = name;
    private string Url { get; } = url;
    private string NiceUrl { get; } = niceUrl;
    public string ArchiveName { get; } = archiveName;
    private string? Md5 { get; } = md5;
    private string DownloadPath => Path.Join(gammaDir, "downloads", ArchiveName);
    private string ExtractPath => Path.Join(gammaDir, "mods", outputDirName);
    private IList<string> Instructions { get; } = instructions;

    public virtual async Task DownloadAsync(CancellationToken cancellationToken = default)
    {
        if (
            Path.Exists(DownloadPath)
                && !string.IsNullOrWhiteSpace(Md5)
                && await Md5Utility.CalculateFileMd5Async(
                    DownloadPath,
                    pct =>
                        gammaProgress.OnProgressChanged(
                            new GammaProgress.GammaInstallProgressEventArgs(Name, "Check MD5", pct)
                        )
                ) != Md5
            || !Path.Exists(DownloadPath)
        )
        {
            await modDbUtility.GetModDbLinkCurl(
                Url,
                DownloadPath,
                pct =>
                    gammaProgress.OnProgressChanged(
                        new GammaProgress.GammaInstallProgressEventArgs(Name, "Download", pct)
                    ),
                cancellationToken
            );
        }
    }

    public virtual async Task ExtractAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(ExtractPath);

        await archiveUtility.ExtractAsync(
            DownloadPath,
            ExtractPath,
            pct =>
                gammaProgress.OnProgressChanged(
                    new GammaProgress.GammaInstallProgressEventArgs(Name, "Extract", pct)
                ),
            ct: cancellationToken
        );

        ProcessInstructions.Process(ExtractPath, Instructions);

        CleanExtractPath.Clean(ExtractPath);

        WriteAddonMetaIni.Write(ExtractPath, ArchiveName, NiceUrl);
    }
}
