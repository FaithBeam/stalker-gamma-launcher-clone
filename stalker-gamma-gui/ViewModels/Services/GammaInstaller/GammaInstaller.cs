using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using stalker_gamma_gui.ViewModels.Tabs.MainTab;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstallerServices;
using stalker_gamma.core.Utilities;

namespace stalker_gamma_gui.Services.GammaInstaller;

public record LocalAndRemoteVersion(string? LocalVersion, string RemoteVersion);

public partial class GammaInstaller(
    ICurlService curlService,
    stalker_gamma.core.Services.GammaInstallerServices.GammaInstaller gammaInstaller,
    AnomalyInstaller anomalyInstaller
// Shortcut.Shortcut shortcut
)
{
    /// <summary>
    /// Checks for G.A.M.M.A. updates.
    /// </summary>
    public async Task<(
        LocalAndRemoteVersion gammaVersions,
        LocalAndRemoteVersion modVersions
    )> CheckGammaData()
    {
        var onlineGammaVersion = (
            await _curlService.GetStringAsync(
                "https://raw.githubusercontent.com/Grokitach/Stalker_GAMMA/main/G.A.M.M.A_definition_version.txt"
            )
        ).Trim();
        string? localGammaVersion = null;
        var versionFile = Path.Combine(_dir, "version.txt");
        if (File.Exists(versionFile))
        {
            localGammaVersion = (await File.ReadAllTextAsync(versionFile)).Trim();
        }

        LocalAndRemoteVersion gammaVersions = new(localGammaVersion, onlineGammaVersion);

        string? localMods = null;
        var modsFile = Path.Combine(_dir, "mods.txt");
        var remoteMods = (
            await _curlService.GetStringAsync("https://stalker-gamma.com/api/list?key=")
        )
            .Trim()
            .ReplaceLineEndings();
        if (File.Exists(modsFile))
        {
            localMods = (await File.ReadAllTextAsync(modsFile)).Trim().ReplaceLineEndings();
        }

        LocalAndRemoteVersion modVersions = new(localMods, remoteMods);

        return (gammaVersions, modVersions);
    }

    public async Task InstallUpdateGammaAsync(
        bool deleteReshadeDlls,
        bool preserveUserLtx,
        IReadOnlyList<ModDownloadExtractProgressVm> modDownloadExtractProgressVms,
        object locker,
        string gammaDir,
        string anomalyDir
    )
    {
        ModDownloadExtractProgressVm? anomaly;
        ModDownloadExtractProgressVm? stalkerGamma;
        ModDownloadExtractProgressVm? gammaLargeFiles;
        ModDownloadExtractProgressVm? gunslinger;
        ModDownloadExtractProgressVm? modpackAddons;
        lock (locker)
        {
            anomaly = modDownloadExtractProgressVms.FirstOrDefault(x => x.AddonName == "Anomaly");
            stalkerGamma = modDownloadExtractProgressVms.First(x => x.AddonName == "Stalker_GAMMA");
            gammaLargeFiles = modDownloadExtractProgressVms.First(x =>
                x.AddonName == "gamma_large_files_v2"
            );
            gunslinger = modDownloadExtractProgressVms.First(x =>
                x.AddonName == "teivaz_anomaly_gunslinger"
            );
            modpackAddons = modDownloadExtractProgressVms.First(x =>
                x.AddonName == "modpack_addons"
            );
        }

        var gammaDownloadsPath = Path.Combine(gammaDir, "downloads");
        var anomalyArchivePath = Path.Combine(gammaDownloadsPath, "anomaly.7z");
        var anomalyTask = anomaly is null
            ? Task.CompletedTask
            : Task.Run(async () =>
                await anomalyInstaller.DownloadAndExtractAsync(
                    anomalyArchivePath,
                    anomalyDir,
                    anomaly.ProgressInterface.Report,
                    anomaly.ProgressInterface.Report,
                    anomaly.ProgressInterface.Report
                )
            );

        var gammaTask = Task.Run(async () =>
            await gammaInstaller.InstallAsync(anomalyDir, anomalyTask, gammaDir, gammaDownloadsPath)
        );

        await Task.WhenAll(anomalyTask, gammaTask);

        await _curlService.DownloadFileAsync(
            "https://stalker-gamma.com/api/list?key=",
            _dir,
            "mods.txt",
            null,
            _dir
        );
    }

    /// <summary>
    /// Downloads G.A.M.M.A. data and updates repositories.
    /// </summary>
    private async Task DownloadGammaData(ModDownloadExtractProgressVm stalkerGamma)
    {
        const string branch = "main";

        var t1 = Task.Run(async () =>
        {
            stalkerGamma.Status = Status.Checking;
            await GitUtility.UpdateGitRepoAsync(
                _dir,
                "Stalker_GAMMA",
                branch,
                onProgress: progress => stalkerGamma.ProgressInterface.Report(progress)
            );
            stalkerGamma.ProgressInterface.Report(100);

            stalkerGamma.Status = Status.Extracting;
            DirUtils.CopyDirectory(
                Path.Combine(_dir, "resources", "Stalker_GAMMA", "G.A.M.M.A"),
                Path.Combine(_dir, "G.A.M.M.A."),
                onProgress: pct => stalkerGamma.ProgressInterface.Report(pct * 100)
            );
            File.Copy(
                Path.Combine(
                    _dir,
                    "resources",
                    "Stalker_GAMMA",
                    "G.A.M.M.A_definition_version.txt"
                ),
                Path.Combine(_dir, "version.txt"),
                true
            );
            stalkerGamma.Status = Status.Done;
        });

        await Task.WhenAll(t1);
    }

    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private readonly ICurlService _curlService = curlService;
}
