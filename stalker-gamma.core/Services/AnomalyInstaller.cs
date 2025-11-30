using System.Text.RegularExpressions;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services;

public partial class AnomalyInstaller(ModDb modDb)
{
    public async Task DownloadAndExtractAsync(
        string archivePath,
        string extractDirectory,
        Action<double> onProgress
    )
    {
        await _modDb.GetModDbLinkCurl(AnomalyMdbUrl, archivePath, onProgress);
        await ExtractAnomalyAsync(archivePath, extractDirectory, onProgress);
    }

    private async Task ExtractAnomalyAsync(
        string archivePath,
        string extractDirectory,
        Action<double> onProgress
    ) => await ArchiveUtility.ExtractWithProgress(archivePath, extractDirectory, onProgress);

    private const string AnomalyMdbUrl = "https://www.moddb.com/downloads/start/277404";
    private readonly ModDb _modDb = modDb;

    [GeneratedRegex(@"(\d+(\.\d+)?)\s*%", RegexOptions.Compiled)]
    private static partial Regex ProgressRx();
}
