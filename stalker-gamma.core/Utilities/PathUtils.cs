namespace stalker_gamma.core.Utilities;

public static class PathUtils
{
    public static string? GetCommonDirectory(string path1, string path2)
    {
        if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
        {
            return null;
        }

        // Normalize paths to handle different separators and relative paths
        var fullPath1 = Path.GetFullPath(path1);
        var fullPath2 = Path.GetFullPath(path2);

        var path1Parts = fullPath1.Split(Path.DirectorySeparatorChar);
        var path2Parts = fullPath2.Split(Path.DirectorySeparatorChar);

        // Find the minimum length to iterate
        var minLength = Math.Min(path1Parts.Length, path2Parts.Length);
        var commonPartsCount = 0;

        for (var i = 0; i < minLength; i++)
        {
            // Use case-insensitive comparison for Windows paths
            if (string.Equals(path1Parts[i], path2Parts[i], StringComparison.OrdinalIgnoreCase))
            {
                commonPartsCount++;
            }
            else
            {
                break;
            }
        }

        // If there are no common parts (e.g., different drives C:\ vs D:\), return null
        if (commonPartsCount == 0)
        {
            return null;
        }

        // Take the common parts and join them back into a path
        var commonPathParts = new string[commonPartsCount];
        Array.Copy(path1Parts, commonPathParts, commonPartsCount);

        if (
            commonPathParts.Length == 1
            && !commonPathParts[0].EndsWith(Path.DirectorySeparatorChar)
        )
        {
            return commonPathParts[0] += Path.DirectorySeparatorChar;
        }

        return string.Join(Path.DirectorySeparatorChar.ToString(), commonPathParts);
    }
}
