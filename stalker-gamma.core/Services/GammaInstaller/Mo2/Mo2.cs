using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.Mo2;

public class Mo2(ProgressService progressService)
{
    public void Setup(
        string dir,
        string modPackName,
        string modPackPath,
        string modOrganizerListFile
    )
    {
        progressService.UpdateProgress(
            """
                Done

            ==================================================================================
                             MO2 Profile Setup                                    
            ==================================================================================

            """
        );

        progressService.UpdateProgress("\tDeleting Default Profile");
        if (Path.Exists(Path.Join(dir, "..", "profiles", "Default")))
        {
            Directory.Delete(Path.Join(dir, "..", "profiles", "Default"));
        }

        progressService.UpdateProgress($"\tCreating MO2 Profile {modPackName}");
        if (!Path.Exists(Path.Join(dir, "..", "profiles", modPackName)))
        {
            Directory.CreateDirectory(Path.Join(dir, "..", "profiles", modPackName));
        }

        foreach (
            var fi in new DirectoryInfo(
                Path.Join(dir, "resources", "profiles_files")
            ).EnumerateFiles()
        )
        {
            fi.CopyTo(Path.Join(dir, "..", "profiles", modPackName, fi.Name), true);
        }
        foreach (
            var di in new DirectoryInfo(
                Path.Join(dir, "resources", "profiles_files")
            ).EnumerateDirectories()
        )
        {
            DirUtils.CopyDirectory(di.FullName, Path.Join(dir, "..", "profiles", modPackName));
        }

        progressService.UpdateProgress("\tCopying modpack modlist.txt file");
        File.Copy(
            Path.Join(modPackPath, modOrganizerListFile),
            Path.Join(dir, "..", "profiles", modPackName, "modlist.txt"),
            true
        );
        progressService.UpdateProgress("\tDone");
    }
}
