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
        IProgress<double> progress,
        bool invalidateMirrorCache = false,
        bool useCurlImpersonate = true,
        params string[]? excludeMirrors
    )
    {
        if (excludeMirrors is not null && excludeMirrors.Length > 0)
        {
            progressService.UpdateProgress(
                $"Excluding mirrors: {string.Join(", ", excludeMirrors)}"
            );
        }
        var mirrorTask = Task.Run(() =>
            _mirrorService.GetMirrorAsync(
                $"{url}/all",
                invalidateMirrorCache,
                excludeMirrors: excludeMirrors ?? []
            )
        );
        var getContentTask = Task.Run(() => _curlService.GetStringAsync(url));
        var results = await Task.WhenAll(mirrorTask, getContentTask);

        var (mirror, content) = (results[0], results[1]);
        var link = WindowLocationRx().Match(content).Groups[1].Value;
        var linkSplit = link.Split('/');

        progressService.UpdateProgress($"\tBest mirror picked: {mirror}");
        linkSplit[6] = mirror;

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
            progress
        );

        return mirror;
    }

    [GeneratedRegex("""window.location.href="(.+)";""")]
    private partial Regex WindowLocationRx();
}
