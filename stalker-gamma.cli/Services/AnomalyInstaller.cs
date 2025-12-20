using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.cli.Services;

public class AnomalyInstaller(ModDb modDb)
{
    public async Task DownloadAndExtractAsync(
        string archivePath,
        string extractDirectory,
        Action<double> onProgress
    )
    {
        // download anomaly if archive does not exist
        if (!File.Exists(archivePath))
        {
            await _modDb.GetModDbLinkCurl(AnomalyMdbUrl, archivePath, onProgress);
        }

        // extract anomaly
        await ArchiveUtility.ExtractAsync(archivePath, extractDirectory, onProgress);
    }

    private const string AnomalyMdbUrl = "https://www.moddb.com/downloads/start/277404";
    private readonly ModDb _modDb = modDb;
}
