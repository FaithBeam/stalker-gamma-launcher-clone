using System.Text.Json;
using stalker_gamma.core.Services.DowngradeModOrganizer.Models.Github;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.DowngradeModOrganizer;

public class DowngradeModOrganizer(ProgressService progressService, VersionService versionService)
{
    public async Task DowngradeAsync(string version = "v2.4.4")
    {
        progressService.UpdateProgress($"Downgrading ModOrganizer to {version}");
        var hc = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com"),
            DefaultRequestHeaders =
            {
                { "User-Agent", $"stalker-gamma-installer/{versionService.GetVersion()}" },
            },
        };
        var getReleaseByTagResponse = await hc.GetAsync(
            $"repos/ModOrganizer2/modorganizer/releases/tags/{version}"
        );
        var getReleaseByTag = await JsonSerializer.DeserializeAsync(
            await getReleaseByTagResponse.Content.ReadAsStreamAsync(),
            jsonTypeInfo: GetReleaseByTagCtx.Default.GetReleaseByTag
        );
        var dlUrl = getReleaseByTag
            ?.Assets?.FirstOrDefault(x =>
                x.Name == $"Mod.Organizer-{(version.StartsWith('v') ? version[1..] : version)}.7z"
            )
            ?.BrowserDownloadUrl;
        if (string.IsNullOrWhiteSpace(dlUrl))
        {
            progressService.UpdateProgress("Failed to find download url");
            return;
        }

        progressService.UpdateProgress("Downloading ModOrganizer");

        var mo2ArchivePath = $"{getReleaseByTag!.Name!}.7z";

        await using (var fs = File.Create(mo2ArchivePath))
        {
            using var response = await hc.GetAsync(dlUrl);
            await response.Content.CopyToAsync(fs);
        }

        progressService.UpdateProgress("Removing previous ModOrganizer installation");
        var dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
        var mo2Path = Path.Join(dir, "..");
        foreach (var folder in _foldersToDelete)
        {
            var path = Path.Join(mo2Path, folder);
            if (!Directory.Exists(path))
            {
                continue;
            }

            new DirectoryInfo(path)
                .GetDirectories("*", SearchOption.AllDirectories)
                .ToList()
                .ForEach(di =>
                {
                    di.Attributes &= ~FileAttributes.ReadOnly;
                    di.GetFiles("*", SearchOption.TopDirectoryOnly)
                        .ToList()
                        .ForEach(fi => fi.IsReadOnly = false);
                });
            Directory.Delete(path, true);
        }
        foreach (var file in _filesToDelete)
        {
            var path = Path.Join(mo2Path, file);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        progressService.UpdateProgress($"Extracting {mo2ArchivePath} to {mo2Path}");
        await ArchiveUtility.ExtractAsync(mo2ArchivePath, mo2Path);
        progressService.UpdateProgress("Finished downgrading ModOrganizer");
    }

    private readonly IReadOnlyList<string> _foldersToDelete =
    [
        "dlls",
        "explorer++",
        "licenses",
        "loot",
        "NCC",
        "platforms",
        "plugins",
        "pythoncore",
        "QtQml",
        "QtQuick.2",
        "resources",
        "styles",
        "stylesheets",
        "translations",
        "tutorials",
    ];

    private readonly IReadOnlyList<string> _filesToDelete =
    [
        "boost_python38-vc142-mt-x64-1_75.dll",
        "dump_running_process.bat",
        "helper.exe",
        "libcrypto-1_1-x64.dll",
        "libffi-7.dll",
        "libssl-1_1-x64.dll",
        "ModOrganizer.exe",
        "nxmhandler.exe",
        "python38.dll",
        "pythoncore.zip",
        "QtWebEngineProcess.exe",
        "uibase.dll",
        "usvfs_proxy_x64.exe",
        "usvfs_proxy_x86.exe",
        "usvfs_x64.dll",
        "usvfs_x86.dll",
    ];
}
