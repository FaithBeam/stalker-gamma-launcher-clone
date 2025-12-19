using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;
using stalker_gamma.core.ViewModels.Tabs.MainTab;

namespace stalker_gamma.core.Services.GammaInstaller.ModpackSpecific;

public class ModpackSpecific(GitUtility gitUtility)
{
    private readonly GitUtility _gitUtility = gitUtility;
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public async Task Install(
        string dir,
        string modsPaths,
        ModDownloadExtractProgressVm gammaLargeFiles,
        ModDownloadExtractProgressVm teivazAnomalyGunslinger,
        ModDownloadExtractProgressVm modpackAddonsVm
    )
    {
        var t1 = Task.Run(async () =>
        {
            gammaLargeFiles.Status = Status.Checking;
            await _gitUtility.UpdateGitRepoAsync(
                _dir,
                "gamma_large_files_v2",
                "main",
                onStatus: status => gammaLargeFiles.Status = status,
                onProgress: progress => gammaLargeFiles.ProgressInterface.Report(progress)
            );

            DirUtils.CopyDirectory(
                Path.Join(dir, "resources", "gamma_large_files_v2"),
                modsPaths,
                onProgress: (count, total) =>
                    gammaLargeFiles.ProgressInterface.Report((double)count / total * 100)
            );
            gammaLargeFiles.Status = Status.Done;
        });

        var t2 = Task.Run(async () =>
        {
            teivazAnomalyGunslinger.Status = Status.Checking;
            await _gitUtility.UpdateGitRepoAsync(
                _dir,
                "teivaz_anomaly_gunslinger",
                "main",
                onStatus: status => teivazAnomalyGunslinger.Status = status,
                onProgress: progress => teivazAnomalyGunslinger.ProgressInterface.Report(progress)
            );

            teivazAnomalyGunslinger.Status = Status.Extracting;
            foreach (
                var gameDataDir in new DirectoryInfo(
                    Path.Join(dir, "resources", "teivaz_anomaly_gunslinger")
                ).EnumerateDirectories("gamedata", SearchOption.AllDirectories)
            )
            {
                DirUtils.CopyDirectory(
                    gameDataDir.FullName,
                    Path.Join(
                        modsPaths,
                        "312- Gunslinger Guns for Anomaly - Teivazcz & Gunslinger Team",
                        "gamedata"
                    ),
                    onProgress: (count, total) =>
                        teivazAnomalyGunslinger.ProgressInterface.Report(
                            (double)count / total * 100
                        )
                );
            }
            teivazAnomalyGunslinger.Status = Status.Done;
        });

        var t3 = Task.Run(() =>
        {
            modpackAddonsVm.Status = Status.Extracting;
            DirUtils.CopyDirectory(
                Path.Join(dir, "G.A.M.M.A", "modpack_addons"),
                modsPaths,
                onProgress: (count, total) =>
                    modpackAddonsVm.ProgressInterface.Report((double)count / total * 100)
            );
            modpackAddonsVm.Status = Status.Done;
        });

        await Task.WhenAll(t1, t2, t3);
    }
}
