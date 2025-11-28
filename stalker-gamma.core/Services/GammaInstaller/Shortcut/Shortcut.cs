namespace stalker_gamma.core.Services.GammaInstaller.Shortcut;

public class Shortcut(IOperatingSystemService operatingSystemService)
{
    private readonly IOperatingSystemService _operatingSystemService = operatingSystemService;

    public void Create(string dir, string modPackPath)
    {
        if (_operatingSystemService.IsWindows())
        {
            CreateShortcutWindows.Create(
                Path.Join(dir, "stalker-gamma-gui.exe"),
                Path.Join(modPackPath, "modpack_data", "modpack_icon.ico"),
                "G.A.M.M.A. Launcher.lnk"
            );
        }
    }
}
