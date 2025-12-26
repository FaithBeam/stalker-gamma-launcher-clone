namespace stalker_gamma.core.Services;

public class WriteSeparatorsService
{
    public async Task WriteAsync(IEnumerable<string> separators)
    {
        foreach (var separator in separators)
        {
            Directory.CreateDirectory(separator);
            await File.WriteAllTextAsync(
                Path.Join(separator, "meta.ini"),
                SeparatorMetaIni.ReplaceLineEndings("\r\n")
            );
        }
    }

    private const string SeparatorMetaIni = """
        [General]
        modid=0
        version=
        newestVersion=
        category=0
        installationFile=

        [installedFiles]
        size=0

        """;
}
