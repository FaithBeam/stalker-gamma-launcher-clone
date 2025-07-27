namespace stalker_gamma.core.Services.GammaInstaller.Utilities;

public static class DirUtils
{
    public static void CopyDirectory(
        string sourceDir,
        string destDir,
        bool overwrite = true,
        string? fileFilter = null
    )
    {
        if (sourceDir.Contains(".git"))
        {
            return;
        }

        Directory.CreateDirectory(destDir);
        var sourceDirInfo = new DirectoryInfo(sourceDir);

        foreach (var file in sourceDirInfo.GetFiles())
        {
            if (!string.IsNullOrWhiteSpace(fileFilter) && file.Name == fileFilter)
            {
                continue;
            }

            if (File.Exists(Path.Combine(destDir, file.Name)))
            {
                if (overwrite)
                {
                    file.CopyTo(Path.Combine(destDir, file.Name), overwrite);
                }
            }
            else
            {
                file.CopyTo(Path.Combine(destDir, file.Name), overwrite);
            }
        }

        foreach (var subDir in sourceDirInfo.GetDirectories())
        {
            // only copy directories if they're not empty
            if (subDir.EnumerateFiles("*.*", SearchOption.AllDirectories).Any())
            {
                CopyDirectory(
                    subDir.FullName,
                    Path.Combine(destDir, subDir.Name),
                    overwrite,
                    fileFilter: fileFilter
                );
            }
        }
    }
}
