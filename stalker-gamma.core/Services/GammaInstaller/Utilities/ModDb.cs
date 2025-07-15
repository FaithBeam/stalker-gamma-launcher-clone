using System.Text.RegularExpressions;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.Utilities;

public partial class ModDb(ProgressService progressService)
{
    /// <summary>
    /// Downloads from ModDB using curl.
    /// </summary>
    public async Task GetModDbLinkCurl(string url, string output, bool useCurlImpersonate = true)
    {
        var content = await Curl.GetStringAsync(url);
        var link = WindowLocationRx().Match(content).Groups[1].Value;
        var linkSplit = link.Split('/');
        var mirror = await Mirror.GetMirror();
        if (string.IsNullOrWhiteSpace(mirror))
        {
            progressService.UpdateProgress("Failed to get mirror from API");
        }
        else
        {
            progressService.UpdateProgress($"\tBest mirror picked: {mirror}");
            linkSplit[6] = mirror;
        }
        var downloadLink = string.Join("/", linkSplit);
        progressService.UpdateProgress($"  Retrieved link: {downloadLink}");
        var parentPath = Directory.GetParent(output);
        if (parentPath is not null && !parentPath.Exists)
        {
            parentPath.Create();
        }
        await Curl.DownloadFileAsync(
            downloadLink,
            parentPath?.FullName ?? "./",
            Path.GetFileName(output),
            useCurlImpersonate
        );
    }

    [GeneratedRegex("""window.location.href="(.+)";""")]
    private partial Regex WindowLocationRx();
}
