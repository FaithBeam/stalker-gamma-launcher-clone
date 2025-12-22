using System.Security.Cryptography;

namespace stalker_gamma.core.Utilities;

public static class Md5Utility
{
    public static async Task<string> CalculateFileMd5Async(
        string filePath,
        Action<double> onProgress
    )
    {
        using var md5 = MD5.Create();
        await using var stream = File.OpenRead(filePath);

        var fileSize = stream.Length;
        var buffer = new byte[81920]; // 80 KB buffer
        long totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            md5.TransformBlock(buffer, 0, bytesRead, null, 0);
            totalBytesRead += bytesRead;
            onProgress.Invoke((double)totalBytesRead / fileSize);
        }

        // Finalize the hash computation
        md5.TransformFinalBlock([], 0, 0);

        return Convert.ToHexStringLower(md5.Hash!);
    }
}
