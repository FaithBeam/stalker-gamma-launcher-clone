using System.Text.RegularExpressions;
using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.Anomaly;

public class Anomaly(ProgressService progressService)
{
    public async Task Patch(
        string dir,
        string modPackPath,
        string modOrganizerListFile,
        bool deleteReshadeDlls,
        bool preserveUserLtx
    )
    {
        progressService.UpdateProgress(
            """

            ==================================================================================
                             Patching Anomaly bin, audio and MCM preferences                  
            ==================================================================================

            """
        );

        var modOrganizerIni = Path.Join(dir, "..", "ModOrganizer.ini");
        progressService.UpdateProgress("\tLocating Anomaly folder from ModOrganizer.ini");
        var mo2Ini = await File.ReadAllTextAsync(modOrganizerIni);
        var anomalyPath =
            Regex
                .Match(mo2Ini, @"\r?\ngamePath=@ByteArray\((.*)\)\r?\n", RegexOptions.IgnoreCase)
                .Groups[1]
                .Value.Replace(@"\\", "\\")
            ?? throw new AnomalyException($"Anomaly folder not found in {modOrganizerIni}");
        progressService.UpdateProgress(
            $"\tCopying user profile, reshade files, and patched exes from {Path.Join(modPackPath, modOrganizerListFile)} to {anomalyPath}"
        );
        DirUtils.CopyDirectory(
            Path.Join(modPackPath, "modpack_patches"),
            anomalyPath,
            fileFilter: preserveUserLtx ? "user.ltx" : null
        );

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

public class AnomalyException(string message) : Exception(message);
