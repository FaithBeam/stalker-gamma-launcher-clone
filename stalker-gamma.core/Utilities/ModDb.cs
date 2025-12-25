using System.Text.RegularExpressions;
using stalker_gamma.core.Services;

namespace stalker_gamma.core.Utilities;

public partial class ModDb(ICurlService curlService, MirrorService mirrorService)
{
    private readonly ICurlService _curlService = curlService;
    private readonly MirrorService _mirrorService = mirrorService;

    /// <summary>
    /// Downloads from ModDB using curl.
    /// </summary>
    public async Task<string?> GetModDbLinkCurl(string url,
        string output,
        Action<double> onProgress,
        CancellationToken? cancellationToken = null,
        bool invalidateMirrorCache = false,
        params string[]? excludeMirrors)
    {
        cancellationToken ??= CancellationToken.None;
        try
        {
            var mirrorTask = Task.Run(() =>
                _mirrorService.GetMirrorAsync(
                    $"{url}/all",
                    invalidateMirrorCache,
                    excludeMirrors: excludeMirrors ?? []
                ), (CancellationToken)cancellationToken);
            var getContentTask = Task.Run(() => _curlService.GetStringAsync(url, cancellationToken), (CancellationToken)cancellationToken);
            var results = await Task.WhenAll(mirrorTask, getContentTask);

            var (mirror, content) = (results[0], results[1]);
            var link = WindowLocationRx().Match(content).Groups[1].Value;
            var linkSplit = link.Split('/');

            linkSplit[6] = mirror;

            var downloadLink = string.Join("/", linkSplit);
            var parentPath = Directory.GetParent(output);
            if (parentPath is not null && !parentPath.Exists)
            {
                parentPath.Create();
            }

            await _curlService.DownloadFileAsync(
                downloadLink,
                parentPath?.FullName ?? "./",
                Path.GetFileName(output),
                onProgress
            );

            return mirror;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [GeneratedRegex("""window.location.href="(.+)";""")]
    private partial Regex WindowLocationRx();
}
