namespace stalker_gamma.core.Services.ModOrganizer;

public class InstallModOrganizerProfile
{
    public async Task InstallAsync(string stalkerGammaRepoPath, string gammaPath)
    {
        var gammaProfilesPath = Path.Join(gammaPath, "profiles", "G.A.M.M.A");
        var settingsPath = Path.Join(gammaProfilesPath, "settings.txt");
        Console.WriteLine($"[+] Installing G.A.M.M.A profile in {gammaProfilesPath}");
        Directory.CreateDirectory(gammaProfilesPath);
        File.Copy(
            Path.Join(stalkerGammaRepoPath, "G.A.M.M.A", "modpack_data", "modlist.txt"),
            Path.Join(gammaProfilesPath, "modlist.txt"),
            true
        );
        await File.WriteAllTextAsync(
            settingsPath,
            """
            [General]
            LocalSaves=false
            LocalSettings=true
            AutomaticArchiveInvalidation=false
            """
        );
    }
}
