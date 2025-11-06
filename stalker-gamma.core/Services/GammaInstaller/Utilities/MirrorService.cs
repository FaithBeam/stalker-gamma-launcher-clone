namespace stalker_gamma.core.Services.GammaInstaller.Utilities;

public class MirrorService(IHttpClientFactory hcf)
{
    private readonly IHttpClientFactory _hcf = hcf;
    private const string MirrorUrl = "https://stalker-gamma.com/api/mirrors";

    public async Task<string?> GetMirror()
    {
        var hc = _hcf.CreateClient();
        var content = await hc.GetStringAsync(MirrorUrl);

        var lines = content.Split('\n');
        var random = new Random();

        return lines
            .Select(line => new { Line = line, Parts = line.Split('\t') })
            .OrderBy(x => double.Parse(x.Parts[^2]))
            .Take(3)
            .OrderBy(_ => random.Next())
            .FirstOrDefault()
            ?.Line.Split('\t')[1];
    }

    public async Task<string?> GetMirror(params string[] excludeMirrors)
    {
        var hc = _hcf.CreateClient();
        var content = await hc.GetStringAsync(MirrorUrl);

        var lines = content.Split('\n');
        var random = new Random();

        return lines
            .Where(line => excludeMirrors.All(em => !line.Contains(em)))
            .Select(line => new { Line = line, Parts = line.Split('\t') })
            .OrderBy(x => double.Parse(x.Parts[^2]))
            .Take(3)
            .OrderBy(_ => random.Next())
            .FirstOrDefault()
            ?.Line.Split('\t')[1];
    }
}
