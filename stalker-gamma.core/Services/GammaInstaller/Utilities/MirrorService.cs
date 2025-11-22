using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace stalker_gamma.core.Services.GammaInstaller.Utilities;

public partial class MirrorService(ICurlService cs)
{
    private readonly ICurlService _cs = cs;
    private FrozenSet<string>? _mirrors;

    public async Task<string> GetMirrorAsync(
        string mirrorUrl,
        bool invalidateCache = false,
        params string[] excludeMirrors
    )
    {
        _mirrors =
            _mirrors is null || invalidateCache ? await GetMirrorsAsync(mirrorUrl) : _mirrors;
        return _mirrors
            .Where(mirror => excludeMirrors.All(em => !mirror.Contains(em)))
            .OrderBy(_ => Guid.NewGuid())
            .First();
    }

    private async Task<FrozenSet<string>> GetMirrorsAsync(string mirrorUrl)
    {
        var mirrorsHtml = await _cs.GetStringAsync(mirrorUrl);
        var matches = HrefRx().Matches(mirrorsHtml);
        return matches
            .Select(m =>
                m.Groups["href"]
                    .Value.Split(
                        '/',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )[3]
            )
            .ToFrozenSet();
    }

    [GeneratedRegex("""<a href="(?<href>.+)" id="downloadon">*?""")]
    private static partial Regex HrefRx();
}
