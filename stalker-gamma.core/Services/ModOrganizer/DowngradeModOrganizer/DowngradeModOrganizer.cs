using System.Text.Json;
using stalker_gamma.core.Services.ModOrganizer.DowngradeModOrganizer.Models.Github;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.ModOrganizer.DowngradeModOrganizer;

public class DowngradeModOrganizer(IHttpClientFactory hcf)
{
    public async Task DowngradeAsync(
        string version = "v2.4.4",
        string cachePath = "",
        string? extractPath = null
    )
    {
        extractPath ??= Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory), "..");
        Directory.CreateDirectory(cachePath);
        Directory.CreateDirectory(extractPath);

        var hc = hcf.CreateClient("githubDlArchive");
        var getReleaseByTagResponse = await hc.GetAsync(
            $"https://api.github.com/repos/ModOrganizer2/modorganizer/releases/tags/{version}"
        );
        var getReleaseByTag = await JsonSerializer.DeserializeAsync<GetReleaseByTag>(
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
            return;
        }

        var mo2ArchivePath = Path.Join(cachePath, $"ModOrganizer.{getReleaseByTag!.Name!}.7z");

        if (!File.Exists(mo2ArchivePath))
        {
            await using var fs = File.Create(mo2ArchivePath);
            using var response = await hc.GetAsync(dlUrl);
            await response.Content.CopyToAsync(fs);
        }

        foreach (var folder in _foldersToDelete)
        {
            var path = Path.Join(extractPath, folder);
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
            var path = Path.Join(extractPath, file);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        await ArchiveUtility.ExtractAsync(mo2ArchivePath, extractPath, (pct) => { });
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
