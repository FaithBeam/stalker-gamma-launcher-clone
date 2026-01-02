using ConsoleAppFramework;
using Serilog;
using stalker_gamma.cli.Utilities;
using Stalker.Gamma.Utilities;

namespace stalker_gamma.cli.Commands;

[RegisterCommands("profiles")]
public partial class ProfilesCmds(ILogger logger)
{
    /// <summary>
    /// Lists all profiles in a gamma installation.
    /// </summary>
    /// <param name="gamma">Gamma install path</param>
    public void List(string gamma)
    {
        var gammaProfilesPath = ProfileUtility.ValidateProfileExists(gamma);
        var profiles = new DirectoryInfo(gammaProfilesPath).GetDirectories();
        foreach (var profile in profiles)
        {
            _logger.Information("{Profile}", profile.Name);
        }
    }

    /// <summary>
    /// Lists all mods in a profile.
    /// </summary>
    /// <param name="profile">Profile name</param>
    /// <param name="gamma">Gamma install path</param>
    [Command("list mods")]
    public async Task ListMods([Argument] string profile, string gamma)
    {
        var gammaProfilesPath = ProfileUtility.ValidateProfileExists(gamma);
        var modListPath = Path.Join(gammaProfilesPath, profile, "modlist.txt");
        var modList = await ModListUtility.GetModListAsync(modListPath);
        foreach (var mod in modList)
        {
            _logger.Information("{Active} | {Mod}", mod.Status.ToString().PadRight(13), mod.Name);
        }
    }

    /// <summary>
    /// Deletes a specified profile from a Gamma installation.
    /// </summary>
    /// <param name="profile">Name of the profile to be deleted.</param>
    /// <param name="gamma">Gamma install path.</param>
    public void Delete([Argument] string profile, string gamma)
    {
        var gammaProfilesPath = Path.Join(gamma, "profiles");
        if (!Directory.Exists(gammaProfilesPath))
        {
            throw new DirectoryNotFoundException($"Directory {gammaProfilesPath} doesn't exist");
        }
        var profilePath = Path.Join(gammaProfilesPath, profile);
        if (!Directory.Exists(profilePath))
        {
            throw new DirectoryNotFoundException($"Directory {profilePath} doesn't exist");
        }
        DirUtils.NormalizePermissions(profilePath);
        Directory.Delete(profilePath, true);
        _logger.Information("Profile {Profile} deleted", profile);
    }

    private readonly ILogger _logger = logger;
}
