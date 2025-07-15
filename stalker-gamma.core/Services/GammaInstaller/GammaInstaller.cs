using System.Text.RegularExpressions;
using CliWrap;
using Microsoft.Win32;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller;

public class GammaInstaller(
    ProgressService progressService,
    GitUtility gitUtility,
    AddonsAndSeparators.AddonsAndSeparators addonsAndSeparators,
    ModpackSpecific.ModpackSpecific modpackSpecific,
    Mo2.Mo2 mo2,
    Anomaly.Anomaly anomaly,
    Shortcut.Shortcut shortcut
)
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    /// <summary>
    /// Checks for G.A.M.M.A. updates.
    /// </summary>
    public async Task<(bool NeedUpdate, bool NeedModDBUpdate)> CheckGammaData(
        string modDbApiKey,
        bool useCurlImpersonate
    )
    {
        bool needUpdate;
        var versionFile = Path.Combine(_dir, "version.txt");
        if (File.Exists(versionFile))
        {
            var installedVersion = await File.ReadAllTextAsync(versionFile);
            var onlineVersion = await Curl.GetStringAsync(
                "https://raw.githubusercontent.com/Grokitach/Stalker_GAMMA/main/G.A.M.M.A_definition_version.txt",
                useCurlImpersonate: useCurlImpersonate
            );

            needUpdate = installedVersion.Trim() != onlineVersion.Trim();
        }
        else
        {
            needUpdate = true;
        }

        bool needModDbUpdate;
        var modsFile = Path.Combine(_dir, "mods.txt");
        if (File.Exists(modsFile))
        {
            var modsHash = await Md5Utility.CalculateFileMd5Async(modsFile);
            var key = modDbApiKey.Replace(" ", "");
            var modsToCheck = await Curl.GetStringAsync(
                $"https://stalker-gamma.com/api/list?key={key}",
                useCurlImpersonate: useCurlImpersonate
            );
            var modsToCheckHash = await Md5Utility.CalculateStringMd5(modsToCheck);

            needModDbUpdate = modsHash != modsToCheckHash;
        }
        else
        {
            needModDbUpdate = true;
        }

        return (needUpdate, needModDbUpdate);
    }

    /// <summary>
    /// Logic to run when the first install initialization button is clicked.
    /// </summary>
    public async Task FirstInstallInitialization()
    {
        // Enable long paths on windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\FileSystem",
                    true
                );
                key?.SetValue("LongPathsEnabled", 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                progressService.UpdateProgress(
                    $"Failed to enable long paths: {ex.Message}. Please run as administrator."
                );
            }
        }

        progressService.UpdateProgress(
            """

            ---------------------------- Launching MO2 ---------------------------
            How to configure MO2 for Stalker Anomaly:
            1. Dismiss the error message.
            2. Click Browse... and select the folder where Anomaly is installed.
                the selected folder should display appdata, tools, bin, gamedata folders...
            3. MO2 should load up showing 0 active add-ons. Quit MO2.

            |  It is recommended to uninstall any Global MO2 instance on your computer.
            |  To do so, open the Windows Start Menu (bottom left)
            |  Type 'Program' > Select 'Add or remove programs'
            |  Look for ModOrganizer2 in the list and uninstall it.

            Once you are done with configuring MO2, click Install/Update G.A.M.M.A.
            """
        );

        await Cli.Wrap(Path.Combine(_dir, "..", "ModOrganizer.exe")).ExecuteAsync();
    }

    public async Task InstallUpdateGammaAsync(
        bool forceGitDownload,
        bool checkMd5,
        bool updateLargeFiles,
        bool forceZipExtraction,
        bool deleteReshadeDlls,
        bool useCurlImpersonate
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

        if (!File.Exists(Path.Join(_dir, "modpack_maker_metadata.txt")))
        {
            progressService.UpdateProgress(
                """
                modpack_maker_metadata.txt file not found in Grok's Modpack Maker root folder, exiting.
                Please copy the modpack_maker_metadata.txt file from a valid modpack to Grok's Modpack Maker root folder, where 02.modpack_maker.bat file is visible.
                """
            );
            return;
        }

        await DownloadGammaData();

        var metadata = (await File.ReadAllTextAsync(Path.Join(_dir, "modpack_maker_metadata.txt")))
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Regex.Match(x, "^.*= (.+)$").Groups[1].Value)
            .ToList();

        var modPackName = metadata[1].Trim().TrimEnd('.');
        var modOrganizerListFile = metadata[3].Trim();
        var modPackAdditionalFiles = metadata[4].Trim();

        var downloadsPath = Path.Join(_dir, "..", "downloads");

        var modsPaths = Path.Join(_dir, "..", "mods");
        var modPackPath = Path.Join(_dir, modPackName);
        progressService.UpdateProgress(
            " Downloading GAMMA mods information from www.stalker-gamma.com"
        );

        await Curl.DownloadFileAsync(
            "https://stalker-gamma.com/api/list?key=",
            _dir,
            "mods.txt",
            false
        );

        var modListFile = Path.Join(_dir, "mods.txt");

        // addons and separators install
        await addonsAndSeparators.Install(
            downloadsPath,
            modsPaths,
            modListFile,
            forceGitDownload,
            checkMd5,
            updateLargeFiles,
            forceZipExtraction,
            useCurlImpersonate
        );

        // modpack specific install
        modpackSpecific.Install(_dir, modPackPath, modPackAdditionalFiles, modsPaths);

        // setup mo2
        mo2.Setup(
            dir: _dir,
            modPackName: modPackName,
            modPackPath: modPackPath,
            modOrganizerListFile: modOrganizerListFile
        );

        // Patch anomaly
        await anomaly.Patch(_dir, modPackPath, modOrganizerListFile, deleteReshadeDlls);

        // create shortcut
        shortcut.Create(_dir, modPackPath);

        progressService.UpdateProgress(
            """
            Installation complete. You can now click Play

            Want to support the author of this modpack? Buy me a coffee <3 https://paypal.me/GrokitachGAMMA

            """
        );

        await Curl.DownloadFileAsync(
            "https://stalker-gamma.com/api/list?key=",
            _dir,
            "mods.txt",
            useCurlImpersonate
        );
        await CheckGammaData("", useCurlImpersonate);
    }

    /// <summary>
    /// Downloads G.A.M.M.A. data and updates repositories.
    /// </summary>
    private async Task DownloadGammaData()
    {
        progressService.UpdateProgress(" Updating Github Repositories");
        const string branch = "main";
        await gitUtility.UpdateGitRepo(
            _dir,
            "Stalker_GAMMA",
            "https://github.com/Grokitach/Stalker_GAMMA",
            branch
        );

        await gitUtility.UpdateGitRepo(
            _dir,
            "gamma_large_files_v2",
            "https://github.com/Grokitach/gamma_large_files_v2",
            "main"
        );
        await gitUtility.UpdateGitRepo(
            _dir,
            "teivaz_anomaly_gunslinger",
            "https://github.com/Grokitach/teivaz_anomaly_gunslinger",
            "main"
        );

        progressService.UpdateProgress(
            " Installing the modpack definition data (installer can hang, be patient)"
        );
        DirUtils.CopyDirectory(
            Path.Combine(_dir, "resources", "Stalker_GAMMA", "G.A.M.M.A"),
            Path.Combine(_dir, "G.A.M.M.A.")
        );
        File.Copy(
            Path.Combine(_dir, "resources", "Stalker_GAMMA", "G.A.M.M.A_definition_version.txt"),
            Path.Combine(_dir, "version.txt"),
            true
        );
        progressService.UpdateProgress(" done");
    }
}
