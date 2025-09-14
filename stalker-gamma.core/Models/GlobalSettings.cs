namespace stalker_gamma.core.Models;

public class GlobalSettings
{
    public bool UseCurlImpersonate { get; set; }
    public string? GammaBackupPath { get; set; }

    public async Task WriteAppSettings()
    {
        await File.WriteAllTextAsync(
            "appsettings.json",
            $$"""
            {
              "useCurlImpersonate": {{UseCurlImpersonate.ToString().ToLower()}}{{(
                string.IsNullOrWhiteSpace(GammaBackupPath)
                    ? ""
                    : $",\n  \"gammaBackupPath\": \"{GammaBackupPath.Replace("\\", @"\\")}\""
            )}}
            }
            """
        );
    }
}
