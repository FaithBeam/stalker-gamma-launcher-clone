using System.Text;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;

public interface IModlistRecord { }

public static class ParseModListRecord
{
    public static IModlistRecord ParseLine(string line, ModDb modDb)
    {
        var lineSplit = line.Split('\t');
        var dlLink = lineSplit[0];

        var instructions = lineSplit.ElementAtOrDefault(1);
        var patch = lineSplit.ElementAtOrDefault(2);
        var addonName = lineSplit.ElementAtOrDefault(3);
        var modDbUrl = lineSplit.ElementAtOrDefault(4);
        var zipName = lineSplit.ElementAtOrDefault(5);
        var md5ModDb = lineSplit.ElementAtOrDefault(6);

        if (lineSplit.Length == 1)
        {
            return new Separator { DlLink = dlLink };
        }

        if (dlLink.Contains("moddb"))
        {
            return new ModDbRecord(modDb)
            {
                DlLink = dlLink,
                Instructions = instructions,
                Patch = patch,
                AddonName = addonName,
                ModDbUrl = modDbUrl,
                ZipName = zipName,
                Md5ModDb = md5ModDb,
            };
        }

        if (dlLink.Contains("github"))
        {
            return new GithubRecord
            {
                DlLink = dlLink,
                Instructions = instructions,
                Patch = patch,
                AddonName = addonName,
                ModDbUrl = modDbUrl,
                ZipName = zipName,
                Md5ModDb = md5ModDb,
            };
        }

        if (dlLink.Contains("gamma_large_files"))
        {
            return new GammaLargeFile
            {
                DlLink = dlLink,
                Instructions = instructions,
                Patch = patch,
                AddonName = addonName,
                ModDbUrl = modDbUrl,
                ZipName = zipName,
                Md5ModDb = md5ModDb,
            };
        }

        throw new Exception($"Invalid modlist record: {line}");
    }
}

public class ModlistRecord : IModlistRecord
{
    public string? DlLink { get; set; }
    public string? Instructions { get; set; }
    public string? Patch { get; set; }
    public string? AddonName { get; set; }
    public string? ModDbUrl { get; set; }
    public string? ZipName { get; set; }
    public string? Md5ModDb { get; set; }
}

public abstract class DownloadableRecord : ModlistRecord
{
    public abstract string Name { get; }
    public string? DlPath { get; set; }
    public string? Dl => DlLink;

    public async Task<bool> ShouldDownloadAsync(string downloadsPath, bool checkMd5)
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
                    return md5 != Md5ModDb;
                }
            }
            else
            {
                // file exists, do not check md5, no need to download again
                return false;
            }
        }

        // file does not exist, yes download
        return true;
    }

    public virtual async Task DownloadAsync(string downloadsPath, bool useCurlImpersonate)
    {
        DlPath ??= Path.Join(downloadsPath, Name);
        if (string.IsNullOrWhiteSpace(Dl))
        {
            throw new Exception($"{nameof(Dl)} is empty");
        }
        await Curl.DownloadFileAsync(
            Dl,
            Path.GetDirectoryName(DlPath) ?? ".",
            Path.GetFileName(DlPath),
            useCurlImpersonate
        );
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

        await ArchiveUtility.ExtractAsync(DlPath, extractPath);

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

public class Separator : ModlistRecord
{
    public string Name => DlLink!;
    public string FolderName => $"{DlLink}_separator";

    public void WriteMetaIni(string modsPaths, int counter)
    {
        if (!Path.Exists(Path.Join(modsPaths, $"{counter}-{FolderName}")))
        {
            Directory.CreateDirectory(Path.Join(modsPaths, $"{counter}-{FolderName}"));
        }
        File.Copy(
            Path.Join("resources", "separator_meta.ini"),
            Path.Join(modsPaths, $"{counter}-{FolderName}", "meta.ini"),
            true
        );
    }
}

public class GithubRecord : DownloadableRecord
{
    public override string Name => $"{DlLink!.Split('/')[4]}.zip";
}

public class GammaLargeFile : DownloadableRecord
{
    public override string Name => $"{DlLink!.Split('/')[6]}.zip";
}

public class ModDbRecord(ModDb modDb) : DownloadableRecord
{
    public override string Name => ZipName!;

    public override async Task DownloadAsync(string downloadsPath, bool useCurlImpersonate)
    {
        DlPath ??= Path.Join(downloadsPath, Name);
        await modDb.GetModDbLinkCurl(DlLink!, DlPath);

        if (await ShouldDownloadAsync(downloadsPath, true))
        {
            await modDb.GetModDbLinkCurl(DlLink!, DlPath);
        }
    }
}
