using System.Text.RegularExpressions;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.Utilities;

public partial class ModDb(
    ProgressService progressService,
    ICurlService curlService,
    MirrorService mirrorService
)
{
    private readonly ICurlService _curlService = curlService;
    private readonly MirrorService _mirrorService = mirrorService;

    /// <summary>
    /// Downloads from ModDB using curl.
    /// </summary>
    public async Task GetModDbLinkCurl(string url, string output, bool useCurlImpersonate = true)
    {
        var content = await _curlService.GetStringAsync(url);
        var link = WindowLocationRx().Match(content).Groups[1].Value;
        var linkSplit = link.Split('/');
        var mirror = await _mirrorService.GetMirror();
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
        await _curlService.DownloadFileAsync(
            downloadLink,
            parentPath?.FullName ?? "./",
            Path.GetFileName(output),
            useCurlImpersonate
        );
    }

    [GeneratedRegex("""window.location.href="(.+)";""")]
    private partial Regex WindowLocationRx();
}
