using System.Text.RegularExpressions;
using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.Anomaly;

public class Anomaly(ProgressService progressService)
{
    public async Task Patch(
        string dir,
        string modPackPath,
        string modOrganizerListFile,
        bool deleteReshadeDlls
    )
    {
        progressService.UpdateProgress(
            """

            ==================================================================================
                             Patching Anomaly bin, audio and MCM preferences                  
            ==================================================================================

            """
        );

        progressService.UpdateProgress("\tLocating Anomaly folder from ModOrganizer.ini");
        var mo2Ini = await File.ReadAllTextAsync(Path.Join(dir, "..", "ModOrganizer.ini"));
        var anomalyPath = Regex
            .Match(
                mo2Ini,
                @"\r?\ngamePath=@ByteArray\((C:\\?\\anomaly)\)\r?\n",
                RegexOptions.IgnoreCase
            )
            .Groups[1]
            .Value.Replace(@"\\", "\\");
        progressService.UpdateProgress(
            $"\tCopying user profile, reshade files, and patched exes from {Path.Join(modPackPath, modOrganizerListFile)} to {anomalyPath}"
        );
        DirUtils.CopyDirectory(Path.Join(modPackPath, "modpack_patches"), anomalyPath);

        if (deleteReshadeDlls)
        {
            progressService.UpdateProgress("\tDeleting reshade dlls");
            List<string> reshadeDlls =
            [
                Path.Join(anomalyPath, "bin", "dxgi.dll"),
                Path.Join(anomalyPath, "bin", "d3d9.dll"),
            ];
            foreach (var reshadeDll in reshadeDlls.Where(Path.Exists))
            {
                File.Delete(reshadeDll);
            }
        }

        progressService.UpdateProgress("\tRemoving shader cache");
        if (Path.Exists(Path.Join(anomalyPath, "appdata", "shaders_cache")))
        {
            Directory.Delete(Path.Join(anomalyPath, "appdata", "shaders_cache"), true);
        }
    }
}
