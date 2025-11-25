using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.ViewModels.Tabs.MainTab;

namespace stalker_gamma.core.Services.GammaInstaller.ModpackSpecific;

public class ModpackSpecific(ProgressService progressService)
{
    public void Install(
        string dir,
        string modPackPath,
        string modPackAdditionalFiles,
        string modsPaths,
        ModDownloadExtractProgressVm gammaLargeFilesVm,
        ModDownloadExtractProgressVm teivazAnomalyGunslingerVm,
        ModDownloadExtractProgressVm modpackAddonsVm
    )
    {
        progressService.UpdateProgress(
            """

            ==================================================================================
                                    Installing Modpack-specific modifications                       
            ==================================================================================

            """
        );

        progressService.UpdateProgress(
            $"\tCopying {Path.Join(modPackPath, modPackAdditionalFiles)} to {modsPaths}, installer can hang but continues working."
        );

        gammaLargeFilesVm.Status = Status.Extracting;
        DirUtils.CopyDirectory(
            Path.Join(dir, "resources", "gamma_large_files_v2"),
            modsPaths,
            onProgress: (count, total) =>
                gammaLargeFilesVm.ExtractProgressInterface.Report((double)count / total * 100)
        );
        gammaLargeFilesVm.Status = Status.Done;

        teivazAnomalyGunslingerVm.Status = Status.Extracting;
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
                    teivazAnomalyGunslingerVm.ExtractProgressInterface.Report(
                        (double)count / total * 100
                    )
            );
        }
        teivazAnomalyGunslingerVm.Status = Status.Done;

        modpackAddonsVm.Status = Status.Extracting;
        DirUtils.CopyDirectory(
            Path.Join(dir, "G.A.M.M.A", "modpack_addons"),
            modsPaths,
            onProgress: (count, total) =>
                modpackAddonsVm.ExtractProgressInterface.Report((double)count / total * 100)
        );
        modpackAddonsVm.Status = Status.Done;
    }
}
