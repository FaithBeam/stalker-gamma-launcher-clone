using System.Text.RegularExpressions;

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
    public async Task<string?> GetModDbLinkCurl(
        string url,
        string output,
        bool useCurlImpersonate = true,
        params string[]? excludeMirrors
    )
    {
        var content = await _curlService.GetStringAsync(url);
        var link = WindowLocationRx().Match(content).Groups[1].Value;
        var linkSplit = link.Split('/');
        if (excludeMirrors is not null && excludeMirrors.Length > 0)
        {
            progressService.UpdateProgress(
                $"Excluding mirrors: {string.Join(", ", excludeMirrors)}"
            );
        }
        var mirror =
            excludeMirrors?.Length == 0
                ? await _mirrorService.GetMirror()
                : await _mirrorService.GetMirror(excludeMirrors!);
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

        return mirror;
    }

    [GeneratedRegex("""window.location.href="(.+)";""")]
    private partial Regex WindowLocationRx();
}
