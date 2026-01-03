using System.Text.Json;
using ConsoleAppFramework;
using Serilog;
using stalker_gamma.cli.Models;
using stalker_gamma.cli.Utilities;
using Stalker.Gamma.Extensions;
using Stalker.Gamma.Factories;
using Stalker.Gamma.GammaInstallerServices;
using Stalker.Gamma.Models;

namespace stalker_gamma.cli.Commands;

[RegisterCommands]
public class UpgradeCmds(
    ILogger logger,
    CliSettings cliSettings,
    StalkerGammaSettings stalkerGammaSettings,
    GammaInstaller gammaInstaller
)
{
    public async Task Upgrade(CancellationToken cancellationToken)
    {
        ValidateActiveProfile.Validate(_logger, _cliSettings.ActiveProfile);
        stalkerGammaSettings.ModpackMakerList = _cliSettings.ActiveProfile!.ModPackMakerUrl;
        
        var anomaly = cliSettings.ActiveProfile!.Anomaly;
        var gamma = cliSettings.ActiveProfile!.Gamma;
        var cache = cliSettings.ActiveProfile!.Cache;
        var mo2Profile = cliSettings.ActiveProfile!.Mo2Profile;
        var modpackMakerUrl = cliSettings.ActiveProfile!.ModPackMakerUrl;
        var modListUrl = cliSettings.ActiveProfile!.ModListUrl;
        stalkerGammaSettings.DownloadThreads = cliSettings.ActiveProfile!.DownloadThreads;
        stalkerGammaSettings.ModpackMakerList = modpackMakerUrl;
        stalkerGammaSettings.ModListUrl = modListUrl;
        
        var resourcesPath = Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory), "resources");
        stalkerGammaSettings.PathToCurl = Path.Join(
            resourcesPath,
            OperatingSystem.IsWindows() ? "curl.exe" : "curl-impersonate"
        );
        stalkerGammaSettings.PathTo7Z = Path.Join(
            resourcesPath,
            OperatingSystem.IsWindows() ? "7zz.exe" : "7zz"
        );
        stalkerGammaSettings.PathToGit = OperatingSystem.IsWindows()
            ? Path.Join(resourcesPath, "git", "cmd", "git.exe")
            : "git";

        await gammaInstaller.UpdateAsync(new InstallUpdatesArgs
        {
Gamma = gamma,
Anomaly = anomaly,
Cache = cache,
CancellationToken = cancellationToken,
Mo2Profile =  mo2Profile,
Mo2Version = 
        });
    }

    private readonly ILogger _logger = logger;
    private readonly CliSettings _cliSettings = cliSettings;
    private readonly IGetStalkerModsFromApi _getStalkerModsFromApi = getStalkerModsFromApi;
    private readonly IModListRecordFactory _modListRecordFactory = modListRecordFactory;
}
