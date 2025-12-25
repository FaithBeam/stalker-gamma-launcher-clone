namespace stalker_gamma.core.Utilities;

public static class DirUtils
{
    public static void NormalizePermissions(string dir)
    {
        var directories = new DirectoryInfo(dir)
            .GetDirectories("*", SearchOption.AllDirectories)
            .ToList();
        directories.ForEach(di =>
        {
            if (!OperatingSystem.IsWindows())
            {
                di.UnixFileMode =
                    UnixFileMode.UserRead
                    | UnixFileMode.UserWrite
                    | UnixFileMode.UserExecute
                    | UnixFileMode.GroupRead
                    | UnixFileMode.GroupExecute
                    | UnixFileMode.GroupWrite
                    | UnixFileMode.OtherRead
                    | UnixFileMode.OtherExecute;
            }
            di.Attributes &= ~FileAttributes.ReadOnly;
            di.GetFiles("*", SearchOption.TopDirectoryOnly)
                .ToList()
                .ForEach(fi => fi.IsReadOnly = false);
        });
    }

    public static void RecursivelyDeleteDirectory(string dir, IReadOnlyList<string> doNotMatch)
    {
        var dirInfo = new DirectoryInfo(dir);
        foreach (
            var d in dirInfo
                .GetDirectories()
                .Where(x => !doNotMatch.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
        )
        {
            d.Delete(true);
        }
    }

    public static void CopyDirectory(
        string sourceDir,
        string destDir,
        bool overwrite = true,
        string? fileFilter = null,
        Action<double>? onProgress = null
    )
    {
        if (sourceDir.Contains(".git"))
        {
            return;
        }

        // Count total files first if progress callback is provided
        int totalFiles = 0;
        int copiedFiles = 0;

        if (onProgress != null)
        {
            totalFiles = CountFiles(sourceDir, fileFilter);
        }

        CopyDirectoryInternal(
            sourceDir,
            destDir,
            overwrite,
            fileFilter,
            onProgress,
            ref copiedFiles,
            totalFiles
        );
    }

    private static int CountFiles(string sourceDir, string? fileFilter)
    {
        if (sourceDir.Contains(".git"))
        {
            return 0;
        }

        var sourceDirInfo = new DirectoryInfo(sourceDir);
        int count = 0;

        foreach (var file in sourceDirInfo.GetFiles())
        {
            if (!string.IsNullOrWhiteSpace(fileFilter) && file.Name == fileFilter)
            {
                continue;
            }
            count++;
        }

        foreach (var subDir in sourceDirInfo.GetDirectories())
        {
            if (subDir.EnumerateFiles("*.*", SearchOption.AllDirectories).Any())
            {
                count += CountFiles(subDir.FullName, fileFilter);
            }
        }

        return count;
    }

    private static void CopyDirectoryInternal(
        string sourceDir,
        string destDir,
        bool overwrite,
        string? fileFilter,
        Action<double>? onProgress,
        ref int copiedFiles,
        int totalFiles
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
                    copiedFiles++;
                    onProgress?.Invoke((double)copiedFiles / totalFiles);
                }
            }
            else
            {
                file.CopyTo(Path.Combine(destDir, file.Name), overwrite);
                copiedFiles++;
                onProgress?.Invoke((double)copiedFiles / totalFiles);
            }
        }

        foreach (var subDir in sourceDirInfo.GetDirectories())
        {
            // only copy directories if they're not empty
            if (subDir.EnumerateFiles("*.*", SearchOption.AllDirectories).Any())
            {
                CopyDirectoryInternal(
                    subDir.FullName,
                    Path.Combine(destDir, subDir.Name),
                    overwrite,
                    fileFilter,
                    onProgress,
                    ref copiedFiles,
                    totalFiles
                );
            }
        }
    }
}
