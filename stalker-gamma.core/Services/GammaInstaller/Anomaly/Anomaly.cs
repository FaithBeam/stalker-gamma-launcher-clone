using System.Text.RegularExpressions;
using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.Anomaly;

public class Anomaly
{
    public async Task Patch(
        string dir,
        string modPackPath,
        string modOrganizerListFile,
        bool deleteReshadeDlls,
        bool preserveUserLtx
    )
    {
        var modOrganizerIni = Path.Join(dir, "..", "ModOrganizer.ini");
        var mo2Ini = await File.ReadAllTextAsync(modOrganizerIni);
        var anomalyPath =
            Regex
                .Match(mo2Ini, @"\r?\ngamePath=@ByteArray\((.*)\)\r?\n", RegexOptions.IgnoreCase)
                .Groups[1]
                .Value.Replace(@"\\", "\\")
            ?? throw new AnomalyException($"Anomaly folder not found in {modOrganizerIni}");
        DirUtils.CopyDirectory(
            Path.Join(modPackPath, "modpack_patches"),
            anomalyPath,
            fileFilter: preserveUserLtx ? "user.ltx" : null
        );

        if (deleteReshadeDlls)
        {
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

        if (Path.Exists(Path.Join(anomalyPath, "appdata", "shaders_cache")))
        {
            Directory.Delete(Path.Join(anomalyPath, "appdata", "shaders_cache"), true);
        }
    }
}

public class AnomalyException(string message) : Exception(message);
