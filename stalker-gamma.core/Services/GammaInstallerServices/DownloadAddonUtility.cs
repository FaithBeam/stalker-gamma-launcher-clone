using stalker_gamma.core.Models;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices;

public class DownloadAddonUtility(DownloadGitHubArchive downloadGitHubArchive)
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="record"></param>
    /// <param name="destination">Can either be a path to a folder or a path to a file archive</param>
    /// <param name="checkMd5Pct"></param>
    /// <param name="downloadPct"></param>
    /// <param name="invalidateMirrorCache"></param>
    public async Task DownloadAsync(
        ModListRecord record,
        string destination,
        Action<double> checkMd5Pct,
        Action<double> downloadPct,
        bool invalidateMirrorCache = false
    )
    {
        if (record is ModDbRecord)
        {
            if (
                Path.Exists(destination)
                    && !string.IsNullOrWhiteSpace(record.Md5ModDb)
                    && await Md5Utility.CalculateFileMd5Async(destination, checkMd5Pct)
                        != record.Md5ModDb
                || !Path.Exists(destination)
            )
            {
                await ModDbUtility.GetModDbLinkCurl(
                    record.DlLink!,
                    destination,
                    downloadPct,
                    invalidateMirrorCache: invalidateMirrorCache
                );
            }
        }
        else if (record is GithubRecord)
        {
            await downloadGitHubArchive.DownloadAsync(record.DlLink!, destination, downloadPct);
        }
    }
}
