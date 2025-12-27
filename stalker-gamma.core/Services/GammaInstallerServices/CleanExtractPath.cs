using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices;

public static class CleanExtractPath
{
    public static void Clean(string extractPath)
    {
        if (!Directory.Exists(extractPath))
        {
            return;
        }

        DirUtils.NormalizePermissions(extractPath);

        DirUtils.RecursivelyDeleteDirectory(extractPath, DoNotMatch);
    }

    private static readonly IReadOnlyList<string> DoNotMatch =
    [
        "gamedata",
        "appdata",
        "db",
        "fomod",
    ];
}
