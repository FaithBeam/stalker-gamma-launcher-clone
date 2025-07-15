namespace stalker_gamma.core.Services.GammaInstaller.Shortcut;

public class Shortcut(ProgressService progressService)
{
    public void Create(string dir, string modPackPath)
    {
        progressService.UpdateProgress(
            """
               Done
               
            ==================================================================================
                       Creating Shortcut to the modpack MO2 instance on the desktop           
            ==================================================================================

            """
        );
        if (OperatingSystem.IsWindows())
        {
            CreateShortcutWindows.Create(
                Path.Join(dir, "stalker-gamma-gui.exe"),
                Path.Join(modPackPath, "modpack_data", "modpack_icon.ico"),
                "G.A.M.M.A. Launcher.lnk"
            );
        }
        else if (OperatingSystem.IsLinux())
        {
            // CreateShortcutLinux.Create(
            //     Path.Join(Dir, "stalker-gamma-gui.exe"),
            //     Path.Join(modPackPath, "modpack_data", "modpack_icon.ico"),
            //     "G.A.M.M.A. Launcher.lnk"
            // );
        }
        else if (OperatingSystem.IsMacOS())
        {
            // CreateShortcutOsx.Create(
            //     Path.Join(Dir, "stalker-gamma-gui.exe"),
            //     Path.Join(modPackPath, "modpack_data", "modpack_icon.ico"),
            //     "G.A.M.M.A. Launcher.lnk"
            // );
        }
    }
}
