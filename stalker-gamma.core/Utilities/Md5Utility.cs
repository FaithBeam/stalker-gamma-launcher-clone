using System.Security.Cryptography;

namespace stalker_gamma.core.Utilities;

public static class Md5Utility
{
    public static async Task<string> CalculateFileMd5Async(string filePath)
    {
        using var md5 = MD5.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await md5.ComputeHashAsync(stream);
        return Convert.ToHexStringLower(hashBytes);
    }

    public static async Task<string> CalculateStringMd5(string content)
    {
        using var md5 = MD5.Create();
        using var ms = new MemoryStream();
        await using var sw = new StreamWriter(ms);
        await sw.WriteAsync(content);
        var hashBytes = await md5.ComputeHashAsync(ms);
        return Convert.ToHexStringLower(hashBytes);
    }
}
