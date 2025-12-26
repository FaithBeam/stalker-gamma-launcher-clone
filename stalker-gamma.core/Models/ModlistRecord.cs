using System.Buffers;
using System.Text;
using stalker_gamma.core.Services;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Models;

public interface IModListRecord;

public class ModListRecord : IModListRecord
{
    public int Counter { get; set; }
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
    public Action<string> OnStatus { get; set; } = _ => { };
    public Action<double> OnMd5Progress { get; set; } = _ => { };
    public Action<double> OnDownloadProgress { get; set; } = _ => { };
    public Action<double> OnExtractProgress { get; set; } = _ => { };

    public enum Action
    {
        DoNothing,
        DownloadForced,
        DownloadMissing,
        DownloadMd5Mismatch,
    }

    public virtual async Task<Action> ShouldDownloadAsync(string downloadsPath)
    {
        DlPath ??= Path.Join(downloadsPath, Name);

        if (File.Exists(DlPath))
        {
            OnStatus("CheckingMd5");
            var md5 = await Md5Utility.CalculateFileMd5Async(
                DlPath,
                onProgress: pct => OnMd5Progress(pct * 100)
            );
            if (!string.IsNullOrWhiteSpace(Md5ModDb))
            {
                // file exists, download if local archive md5 does not match md5moddb
                return md5 == Md5ModDb ? Action.DoNothing : Action.DownloadMd5Mismatch;
            }
        }

        // file does not exist, yes download
        return Action.DownloadMissing;
    }

    public virtual async Task DownloadAsync(
        string downloadsPath,
        bool invalidateMirrorCache = false
    )
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
            OnDownloadProgress,
            _dir
        );
    }

    public async Task ExtractAsync(string downloadsPath, string extractPath)
    {
        DlPath ??= Path.Join(downloadsPath, Name);

        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(extractPath);
        }

        if (string.IsNullOrWhiteSpace(DlPath))
        {
            throw new DownloadableRecordException($"{nameof(DlPath)} is empty");
        }

        await ArchiveUtility.ExtractAsync(DlPath, extractPath, OnExtractProgress);

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
        ProcessInstructions(extractPath, instructionsSplit);

        CleanExtractPath(extractPath);
    }

    private static void ProcessInstructions(string extractPath, string[] instructionsSplit)
    {
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
    }

    public void CleanExtractPath(string extractPath)
    {
        if (!Directory.Exists(extractPath))
        {
            return;
        }

        RemoveReadOnlyFlags(extractPath);

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

    private static void RemoveReadOnlyFlags(string extractPath)
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
    }

    private static readonly IReadOnlyList<string> DoNotMatch =
    [
        "gamedata",
        "appdata",
        "db",
        "fomod",
    ];
}

public class GitRecord : ModListRecord;

public class ModpackSpecific : ModListRecord;

public class Separator : ModListRecord
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public string Name => DlLink!;
    public string FolderName => $"{Counter}- {DlLink}_separator";

    public void WriteMetaIni(string modsPaths)
    {
        if (!Path.Exists(Path.Join(modsPaths, FolderName)))
        {
            Directory.CreateDirectory(Path.Join(modsPaths, FolderName));
        }
        File.Copy(
            Path.Join(_dir, "resources", "separator_meta.ini"),
            Path.Join(modsPaths, FolderName, "meta.ini"),
            true
        );
    }
}

public class GithubRecord(ICurlService curlService, IHttpClientFactory hcf)
    : DownloadableRecord(curlService)
{
    private readonly HttpClient _hc = hcf.CreateClient("githubDlArchive");
    public override string Name => $"{DlLink!.Split('/')[4]}.zip";
    private const int BufferSize = 1024 * 1024;

    public override async Task DownloadAsync(
        string downloadsPath,
        bool invalidateMirrorCache = false
    )
    {
        DlPath ??= Path.Join(downloadsPath, Name);
        if (string.IsNullOrWhiteSpace(Dl))
        {
            throw new Exception($"{nameof(Dl)} is empty");
        }

        if (!Directory.Exists(downloadsPath))
        {
            Directory.CreateDirectory(downloadsPath);
        }

        using var response = await _hc.GetAsync(Dl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;

        await using var fs = new FileStream(
            DlPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: BufferSize
        );
        await using var contentStream = await response.Content.ReadAsStreamAsync();

        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            long totalBytesRead = 0;
            int bytesRead;

            while (
                (bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0
            )
            {
                await fs.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;

                if (totalBytes.HasValue)
                {
                    var progressPercentage = (double)totalBytesRead / totalBytes.Value * 100.0;
                    OnDownloadProgress(progressPercentage);
                }
            }

            OnDownloadProgress(100);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

public class GammaLargeFile(ICurlService curlService) : DownloadableRecord(curlService)
{
    public override string Name => $"{DlLink!.Split('/')[6]}.zip";
}

public class ModDbRecord(ModDb modDb, ICurlService curlService) : DownloadableRecord(curlService)
{
    public override string Name => ZipName!;
    private readonly List<string> _visitedMirrors = [];

    public override async Task DownloadAsync(
        string downloadsPath,
        bool invalidateMirrorCache = false
    )
    {
        DlPath ??= Path.Join(downloadsPath, Name);
        var mirror = await modDb.GetModDbLinkCurl(
            DlLink!,
            DlPath,
            OnDownloadProgress,
            invalidateMirrorCache: invalidateMirrorCache,
            excludeMirrors: _visitedMirrors.ToArray()
        );
        if (!string.IsNullOrWhiteSpace(mirror))
        {
            _visitedMirrors.Add(mirror);
        }

        if (
            await ShouldDownloadAsync(downloadsPath)
            is Action.DownloadMissing
                or Action.DownloadMd5Mismatch
        )
        {
            await modDb.GetModDbLinkCurl(DlLink!, DlPath, OnDownloadProgress);
        }
    }
}

public class DownloadableRecordException : Exception
{
    public DownloadableRecordException(string message)
        : base(message) { }

    public DownloadableRecordException(string message, Exception innerException)
        : base(message, innerException) { }
}
