using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.EventStream;
using CliWrap.Exceptions;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;
using stalker_gamma.core.ViewModels.Tabs.MainTab;

namespace stalker_gamma.core.Services.GammaInstaller;

public record LocalAndRemoteVersion(string? LocalVersion, string RemoteVersion);

public partial class GammaInstaller(
    ICurlService curlService,
    GitUtility gitUtility,
    AddonsAndSeparators.AddonsAndSeparators addonsAndSeparators,
    ModpackSpecific.ModpackSpecific modpackSpecific,
    Mo2.Mo2 mo2,
    Anomaly.Anomaly anomaly,
    Shortcut.Shortcut shortcut
)
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private readonly ICurlService _curlService = curlService;

    /// <summary>
    /// Checks for G.A.M.M.A. updates.
    /// </summary>
    public async Task<(
        LocalAndRemoteVersion gammaVersions,
        LocalAndRemoteVersion modVersions
    )> CheckGammaData()
    {
        var onlineGammaVersion = (
            await _curlService.GetStringAsync(
                "https://raw.githubusercontent.com/Grokitach/Stalker_GAMMA/main/G.A.M.M.A_definition_version.txt"
            )
        ).Trim();
        string? localGammaVersion = null;
        var versionFile = Path.Combine(_dir, "version.txt");
        if (File.Exists(versionFile))
        {
            localGammaVersion = (await File.ReadAllTextAsync(versionFile)).Trim();
        }

        LocalAndRemoteVersion gammaVersions = new(localGammaVersion, onlineGammaVersion);

        string? localMods = null;
        var modsFile = Path.Combine(_dir, "mods.txt");
        var remoteMods = (
            await _curlService.GetStringAsync("https://stalker-gamma.com/api/list?key=")
        )
            .Trim()
            .ReplaceLineEndings();
        if (File.Exists(modsFile))
        {
            localMods = (await File.ReadAllTextAsync(modsFile)).Trim().ReplaceLineEndings();
        }

        LocalAndRemoteVersion modVersions = new(localMods, remoteMods);

        return (gammaVersions, modVersions);
    }

    /// <summary>
    /// Logic to run when the first install initialization button is clicked.
    /// </summary>
    public async Task FirstInstallInitialization()
    {
        var stdOutSb = new StringBuilder();
        var stdErrSb = new StringBuilder();

        try
        {
            await Cli.Wrap(Path.Combine(_dir, "..", "ModOrganizer.exe"))
                .Observe()
                .ForEachAsync(cmdEvt =>
                {
                    switch (cmdEvt)
                    {
                        case ExitedCommandEvent exit:
                            if (exit.ExitCode != 0)
                            {
                                throw new ModOrganizerServiceException(
                                    $"""

                                    Exit Code: {exit.ExitCode}
                                    StdErr:  {stdErrSb}
                                    StdOut: {stdOutSb}
                                    """
                                );
                            }

                            break;
                        case StandardErrorCommandEvent stdErr:
                            stdErrSb.AppendLine(stdErr.Text);
                            break;
                        case StandardOutputCommandEvent stdOut:
                            stdOutSb.AppendLine(stdOut.Text);
                            break;
                    }
                });
        }
        catch (CommandExecutionException e)
        {
            throw new ModOrganizerServiceException(
                $"""

                StdErr:  {stdErrSb}
                StdOut: {stdOutSb}
                """,
                e
            );
        }
    }

    public async Task InstallUpdateGammaAsync(
        bool deleteReshadeDlls,
        bool preserveUserLtx,
        IReadOnlyList<ModDownloadExtractProgressVm> modDownloadExtractProgressVms,
        object locker
    )
    {
        if (Directory.Exists(Path.Join(_dir, ".modpack_installer.log")))
        {
            File.Move(
                Path.Join(_dir, ".modpack_installer.log"),
                Path.Join(_dir, ".modpack_installer.log.bak"),
                true
            );
        }

        ModDownloadExtractProgressVm? stalkerGamma;
        ModDownloadExtractProgressVm? gammaLargeFiles;
        ModDownloadExtractProgressVm? gunslinger;
        ModDownloadExtractProgressVm? modpackAddons;
        lock (locker)
        {
            stalkerGamma = modDownloadExtractProgressVms.First(x => x.AddonName == "Stalker_GAMMA");
            gammaLargeFiles = modDownloadExtractProgressVms.First(x =>
                x.AddonName == "gamma_large_files_v2"
            );
            gunslinger = modDownloadExtractProgressVms.First(x =>
                x.AddonName == "teivaz_anomaly_gunslinger"
            );
            modpackAddons = modDownloadExtractProgressVms.First(x =>
                x.AddonName == "modpack_addons"
            );
        }
        await DownloadGammaData(stalkerGamma);

        var metadata = (await File.ReadAllTextAsync(Path.Join(_dir, "modpack_maker_metadata.txt")))
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => ModpackMakerRx().Match(x).Groups[1].Value)
            .ToList();

        var modPackName = metadata[1].Trim().TrimEnd('.');
        var modOrganizerListFile = metadata[3].Trim();

        var downloadsPath = Path.GetFullPath(Path.Join(_dir, "..", "downloads"));

        var modsPaths = Path.GetFullPath(Path.Join(_dir, "..", "mods"));
        var modPackPath = Path.Join(_dir, modPackName);

        // addons and separators install
        await addonsAndSeparators.Install(
            downloadsPath,
            modsPaths,
            modDownloadExtractProgressVms
                .Where(x => x.ModListRecord is DownloadableRecord or Separator)
                .ToList()
        );

        // modpack specific install
        await modpackSpecific.Install(_dir, modsPaths, gammaLargeFiles, gunslinger, modpackAddons);

        // setup mo2
        mo2.Setup(
            dir: _dir,
            modPackName: modPackName,
            modPackPath: modPackPath,
            modOrganizerListFile: modOrganizerListFile
        );

        // Patch anomaly
        await anomaly.Patch(
            _dir,
            modPackPath,
            modOrganizerListFile,
            deleteReshadeDlls,
            preserveUserLtx
        );

        // create shortcut
        shortcut.Create(_dir, modPackPath);

        await _curlService.DownloadFileAsync(
            "https://stalker-gamma.com/api/list?key=",
            _dir,
            "mods.txt",
            null,
            _dir
        );
    }

    /// <summary>
    /// Downloads G.A.M.M.A. data and updates repositories.
    /// </summary>
    private async Task DownloadGammaData(ModDownloadExtractProgressVm stalkerGamma)
    {
        const string branch = "main";

        var t1 = Task.Run(async () =>
        {
            stalkerGamma.Status = Status.Checking;
            await gitUtility.UpdateGitRepo(
                _dir,
                "Stalker_GAMMA",
                "https://github.com/Grokitach/Stalker_GAMMA",
                branch,
                onProgress: progress => stalkerGamma.ProgressInterface.Report(progress)
            );
            stalkerGamma.ProgressInterface.Report(100);

            stalkerGamma.Status = Status.Extracting;
            DirUtils.CopyDirectory(
                Path.Combine(_dir, "resources", "Stalker_GAMMA", "G.A.M.M.A"),
                Path.Combine(_dir, "G.A.M.M.A."),
                onProgress: (copied, total) =>
                    stalkerGamma.ProgressInterface.Report((double)copied / total * 100)
            );
            File.Copy(
                Path.Combine(
                    _dir,
                    "resources",
                    "Stalker_GAMMA",
                    "G.A.M.M.A_definition_version.txt"
                ),
                Path.Combine(_dir, "version.txt"),
                true
            );
            stalkerGamma.Status = Status.Done;
        });

        await Task.WhenAll(t1);
    }

    [GeneratedRegex("^.*= (.+)$")]
    private static partial Regex ModpackMakerRx();
}
