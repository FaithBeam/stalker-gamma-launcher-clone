using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices;

public static class DeleteShaderCache
{
    public static void Delete(string anomalyPath)
    {
        var appDataPath = Path.Join(anomalyPath, "appdata");
        var shaderCachePath = Path.Join(appDataPath, "shaders_cache");
        if (Directory.Exists(shaderCachePath))
        {
            DirUtils.NormalizePermissions(shaderCachePath);
            DirUtils.RecursivelyDeleteDirectory(shaderCachePath, []);
        }
    }
}
