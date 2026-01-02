using ConsoleAppFramework;
using Serilog;
using stalker_gamma.cli.Utilities;

namespace stalker_gamma.cli.Commands;

[RegisterCommands("mod")]
public class ModCmds(ILogger logger)
{
    /// <summary>
    /// Show mod status info
    /// </summary>
    /// <param name="mod">Mod name</param>
    /// <param name="profile">Profile name</param>
    /// <param name="gamma">The gamma directory</param>
    public async Task Status([Argument] string mod, string profile, string gamma)
    {
        var gammaProfilesPath = ProfileUtility.ValidateProfileExists(gamma);
        var modListPath = Path.Join(gammaProfilesPath, profile, "modlist.txt");
        var modList = await ModListUtility.GetModListAsync(modListPath);
        var foundMod = modList.FirstOrDefault(x => x.Name == mod);
        if (foundMod is null)
        {
            throw new FileNotFoundException($"Mod {mod} not found in profile {profile}");
        }
        _logger.Information(
            "{Status} | {Name}",
            foundMod.Status.ToString().PadRight(13),
            foundMod.Name
        );
    }

    /// <summary>
    /// Enables a specified mod within a given profile.
    /// </summary>
    /// <param name="profile">The profile name where the mod exists.</param>
    /// <param name="mod">The name of the mod to enable.</param>
    /// <param name="gamma">The gamma directory</param>
    public async Task Enable([Argument] string mod, string profile, string gamma)
    {
        var gammaProfilesPath = ProfileUtility.ValidateProfileExists(gamma);
        var modListPath = Path.Join(gammaProfilesPath, profile, "modlist.txt");
        var modList = await ModListUtility.GetModListAsync(modListPath);
        var foundMod = modList.FirstOrDefault(x => x.Name == mod);
        if (foundMod is null)
        {
            throw new FileNotFoundException($"Mod {mod} not found in profile {profile}");
        }

        foundMod.Status = ModStatus.Enabled;
        await ModListUtility.SaveModListAsync(modListPath, modList);
        _logger.Information(
            "{Status} | {Name}",
            foundMod.Status.ToString().PadRight(13),
            foundMod.Name
        );
    }

    /// <summary>
    /// Disables a specified mod in the given profile
    /// </summary>
    /// <param name="mod">The name of the mod to be disabled.</param>
    /// <param name="profile">The name of the profile in which the mod exists.</param>
    /// <param name="gamma">The gamma directory.</param>
    public async Task Disable([Argument] string mod, string profile, string gamma)
    {
        var gammaProfilesPath = ProfileUtility.ValidateProfileExists(gamma);
        var modListPath = Path.Join(gammaProfilesPath, profile, "modlist.txt");
        var modList = await ModListUtility.GetModListAsync(modListPath);
        var foundMod = modList.FirstOrDefault(x => x.Name == mod);
        if (foundMod is null)
        {
            throw new FileNotFoundException($"Mod {mod} not found in profile {profile}");
        }

        foundMod.Status = ModStatus.Disabled;
        await ModListUtility.SaveModListAsync(modListPath, modList);
        _logger.Information(
            "{Status} | {Name}",
            foundMod.Status.ToString().PadRight(13),
            foundMod.Name
        );
    }

    /// <summary>
    /// Deletes a specified mod in the provided profile.
    /// </summary>
    /// <param name="mod">The name of the mod to delete.</param>
    /// <param name="profile">The name of the profile from which the mod will be deleted.</param>
    /// <param name="gamma">The gamma directory.</param>
    public async Task Delete([Argument] string mod, string profile, string gamma)
    {
        var gammaProfilesPath = ProfileUtility.ValidateProfileExists(gamma);
        var modListPath = Path.Join(gammaProfilesPath, profile, "modlist.txt");
        var modList = await ModListUtility.GetModListAsync(modListPath);
        var foundMod = modList.FirstOrDefault(x => x.Name == mod);
        if (foundMod is null)
        {
            throw new FileNotFoundException($"Mod {mod} not found in profile {profile}");
        }
        modList.Remove(foundMod);
        await ModListUtility.SaveModListAsync(modListPath, modList);
        _logger.Information("Mod {Mod} deleted from profile {Profile}", mod, profile);
    }

    private readonly ILogger _logger = logger;
}
