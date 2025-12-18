namespace stalker_gamma.core.Services.ModOrganizer;

public class DisableNexusModHandlerLink
{
    public async Task DisableAsync(string gammaPath)
    {
        await File.WriteAllTextAsync(
            Path.Join(gammaPath, "nxmhandler.ini"),
            """
            [General]
            noregister=true

            """
        );
    }
}
