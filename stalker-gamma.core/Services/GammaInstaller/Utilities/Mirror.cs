using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.Utilities;

public static class Mirror
{
    public static async Task<string?> GetMirror()
    {
        var content = await Curl.GetStringAsync("https://stalker-gamma.com/api/mirrors");

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
}
