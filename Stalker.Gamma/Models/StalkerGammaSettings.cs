namespace Stalker.Gamma.Models;

public class StalkerGammaSettings
{
    public string ListStalkerModsUrl { get; set; } = "https://stalker-gamma.com/api/list";
    public string GammaLargeFilesRepo { get; set; } =
        "https://github.com/Grokitach/gamma_large_files_v2";
    public string GammaSetupRepo { get; set; } = "https://github.com/Grokitach/gamma_setup";
    public string StalkerGammaRepo { get; set; } = "https://github.com/Grokitach/Stalker_GAMMA";
    public string TeivazAnomalyGunslingerRepo { get; set; } =
        "https://github.com/Grokitach/teivaz_anomaly_gunslinger";
    public int DownloadThreads { get; set; } = 1;
    public string PathToUnzip = "unzip";
    public string PathTo7Z = OperatingSystem.IsWindows() ? "7zz.exe" : "7zz";
    public string PathToCurl = OperatingSystem.IsWindows() ? "curl.exe" : "curl-impersonate";
    public string PathToTar = "tar";
    public string PathToGit = OperatingSystem.IsWindows() ? "git.exe" : "git";
}
