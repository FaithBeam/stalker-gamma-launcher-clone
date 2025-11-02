using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using CliWrap.EventStream;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;

public interface IModListRecord;

public class ModListRecord : IModListRecord
{
    public string? DlLink { get; set; }
    public string? Instructions { get; set; }
    public string? Patch { get; set; }
    public string? AddonName { get; set; }
    public string? ModDbUrl { get; set; }
    public string? ZipName { get; set; }
    public string? Md5ModDb { get; set; }
}

public abstract class DownloadableRecord(ICurlService curlService) : ModListRecord
{
    protected readonly ICurlService CurlService = curlService;
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    public abstract string Name { get; }
    public string? DlPath { get; set; }
    public string? Dl => DlLink;

    public enum Action
    {
        DoNothing,
        DownloadForced,
        DownloadMissing,
        DownloadMd5Mismatch,
    }

    public virtual async Task<Action> ShouldDownloadAsync(
        string downloadsPath,
        bool checkMd5,
        bool forceGitDownload
    )
    {
        DlPath ??= Path.Join(downloadsPath, Name);

        if (File.Exists(DlPath))
        {
            if (checkMd5)
            {
                var md5 = await Md5Utility.CalculateFileMd5Async(DlPath);
                if (!string.IsNullOrWhiteSpace(Md5ModDb))
                {
                    // file exists, download if local archive md5 does not match md5moddb
                    return md5 == Md5ModDb ? Action.DoNothing : Action.DownloadMd5Mismatch;
                }
            }
            else
            {
                // file exists, do not check md5, no need to download again
                return Action.DoNothing;
            }
        }

        // file does not exist, yes download
        return Action.DownloadMissing;
    }

    public virtual async Task<bool> DownloadAsync(string downloadsPath, bool useCurlImpersonate)
    {
        DlPath ??= Path.Join(downloadsPath, Name);
        if (string.IsNullOrWhiteSpace(Dl))
        {
            throw new Exception($"{nameof(Dl)} is empty");
        }
        await CurlService.DownloadFileAsync(
            Dl,
            Path.GetDirectoryName(DlPath) ?? ".",
            Path.GetFileName(DlPath),
            useCurlImpersonate,
            _dir
        );
        return true;
    }

    public async Task ExtractAsync(string extractPath)
    {
        if (Path.Exists(extractPath))
        {
            new DirectoryInfo(extractPath)
                .GetDirectories("*", SearchOption.AllDirectories)
                .ToList()
                .ForEach(di =>
                {
                    di.Attributes &= ~FileAttributes.ReadOnly;
                    di.GetFiles("*", SearchOption.TopDirectoryOnly)
                        .ToList()
                        .ForEach(fi => fi.IsReadOnly = false);
                });
            Directory.Delete(extractPath, true);
            Directory.CreateDirectory(extractPath);
        }

        if (string.IsNullOrWhiteSpace(DlPath))
        {
            throw new Exception($"{nameof(DlPath)} is empty");
        }

        await ArchiveUtility
            .Extract(DlPath, extractPath)
            .ForEachAsync(cmdEvt =>
            {
                switch (cmdEvt)
                {
                    case ExitedCommandEvent exit:
                        Debug.WriteLine($"Exit: {exit.ExitCode}");
                        break;
                    case StandardErrorCommandEvent stdErr:
                        Debug.WriteLine($"Error: {stdErr.Text}");
                        break;
                    case StandardOutputCommandEvent stdOut:
                        Debug.WriteLine($"Out: {stdOut.Text}");
                        break;
                    case StartedCommandEvent start:
                        Debug.WriteLine($"Start: {start.ProcessId}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(cmdEvt));
                }
            });

        SolveInstructions(extractPath);
    }

    public async Task WriteMetaIniAsync(string extractPath) =>
        await File.WriteAllTextAsync(
            Path.Join(extractPath, "meta.ini"),
            $"""
            [General]
            gameName=stalkeranomaly
            modid=0
            ignoredversion={Name}
            version={Name}
            newestversion={Name}
            category="-1,"
            nexusFileStatus=1
            installationFile={Name}
            repository=
            comments=
            notes=
            nexusDescription=
            url={ModDbUrl}
            hasCustomURL=true
            lastNexusQuery=
            lastNexusUpdate=
            nexusLastModified=2021-11-09T18:10:18Z
            converted=false
            validated=false
            color=@Variant(\0\0\0\x43\0\xff\xff\0\0\0\0\0\0\0\0)
            tracked=0

            [installedFiles]
            1\modid=0
            1\fileid=0
            size=1

            """,
            encoding: Encoding.UTF8
        );

    private void SolveInstructions(string extractPath)
    {
        if (string.IsNullOrWhiteSpace(Instructions) || Instructions == "0")
        {
            return;
        }

        var instructionsSplit = Instructions.Split(':');
        foreach (var i in instructionsSplit)
        {
            if (Path.Exists(Path.Join(extractPath, i, "gamedata")))
            {
                DirUtils.CopyDirectory(Path.Join(extractPath, i), extractPath);
            }
            else
            {
                Directory.CreateDirectory(Path.Join(extractPath, "gamedata"));
                if (Directory.Exists(Path.Join(extractPath, i)))
                {
                    DirUtils.CopyDirectory(
                        Path.Join(extractPath, i),
                        Path.Join(extractPath, "gamedata")
                    );
                }
            }
        }

        CleanExtractPath(extractPath);
    }

    public void CleanExtractPath(string extractPath)
    {
        if (!Directory.Exists(extractPath))
        {
            return;
        }

        new DirectoryInfo(extractPath)
            .GetDirectories("*", SearchOption.AllDirectories)
            .ToList()
            .ForEach(di =>
            {
                di.Attributes &= ~FileAttributes.ReadOnly;
                di.GetFiles("*", SearchOption.TopDirectoryOnly)
                    .ToList()
                    .ForEach(fi => fi.IsReadOnly = false);
            });

        var dirInfo = new DirectoryInfo(extractPath);
        foreach (
            var d in dirInfo
                .GetDirectories()
                .Where(x => !DoNotMatch.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
        )
        {
            d.Delete(true);
        }
    }

    private static readonly IReadOnlyList<string> DoNotMatch =
    [
        "gamedata",
        "appdata",
        "db",
        "fomod",
    ];
}

public class Separator : ModListRecord
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public string Name => DlLink!;
    public string FolderName => $"{DlLink}_separator";

    public void WriteMetaIni(string modsPaths, int counter)
    {
        if (!Path.Exists(Path.Join(modsPaths, $"{counter}-{FolderName}")))
        {
            Directory.CreateDirectory(Path.Join(modsPaths, $"{counter}-{FolderName}"));
        }
        File.Copy(
            Path.Join(_dir, "resources", "separator_meta.ini"),
            Path.Join(modsPaths, $"{counter}-{FolderName}", "meta.ini"),
            true
        );
    }
}

public class GithubRecord(ICurlService curlService) : DownloadableRecord(curlService)
{
    public override string Name => $"{DlLink!.Split('/')[4]}.zip";

    public override async Task<Action> ShouldDownloadAsync(
        string downloadsPath,
        bool checkMd5,
        bool forceGitDownload
    )
    {
        if (forceGitDownload)
        {
            return Action.DownloadForced;
        }

        return await base.ShouldDownloadAsync(downloadsPath, checkMd5, forceGitDownload);
    }
}

public class GammaLargeFile(ICurlService curlService) : DownloadableRecord(curlService)
{
    public override string Name => $"{DlLink!.Split('/')[6]}.zip";
}

public class ModDbRecord(ModDb modDb, ICurlService curlService) : DownloadableRecord(curlService)
{
    public override string Name => ZipName!;

    public override async Task<bool> DownloadAsync(string downloadsPath, bool useCurlImpersonate)
    {
        DlPath ??= Path.Join(downloadsPath, Name);
        await modDb.GetModDbLinkCurl(DlLink!, DlPath);

        if (
            await ShouldDownloadAsync(downloadsPath, true, false)
            is Action.DownloadMissing
                or Action.DownloadMd5Mismatch
        )
        {
            await modDb.GetModDbLinkCurl(DlLink!, DlPath);
        }

        return true;
    }
}
