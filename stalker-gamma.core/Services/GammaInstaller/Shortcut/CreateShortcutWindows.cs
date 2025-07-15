using WindowsShortcutFactory;

namespace stalker_gamma.core.Services.GammaInstaller.Shortcut;

public static class CreateShortcutWindows
{
    public static void Create(string exePath, string iconPath, string shortCutName)
    {
        var savePath = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            shortCutName
        );
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        using var shortcut = new WindowsShortcut();
        shortcut.Path = exePath;
        shortcut.IconLocation = iconPath;
        shortcut.Save(savePath);
    }
}
