using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.ModpackSpecific;

public class ModpackSpecific(ProgressService progressService)
{
    public void Install(
        string dir,
        string modPackPath,
        string modPackAdditionalFiles,
        string modsPaths
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

        DirUtils.CopyDirectory(Path.Join(dir, "resources", "gamma_large_files_v2"), modsPaths);

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
                )
            );
        }

        DirUtils.CopyDirectory(Path.Join(dir, "G.A.M.M.A", "modpack_addons"), modsPaths);
    }
}
