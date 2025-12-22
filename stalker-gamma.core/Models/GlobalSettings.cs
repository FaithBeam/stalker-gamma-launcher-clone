using System.Text.Json;
using System.Text.Json.Serialization;

namespace stalker_gamma.core.Models;

public class GlobalSettings
{
    [JsonPropertyName("downloadThreads")]
    public int DownloadThreads { get; set; } = 4;

    [JsonPropertyName("extractThreads")]
    public int ExtractThreads { get; set; } = 4;

    [JsonPropertyName("gammaBackupPath")]
    public string? GammaBackupPath { get; set; }

    [JsonPropertyName("checkForLauncherUpdates")]
    public bool CheckForLauncherUpdates { get; set; } = true;

    [JsonPropertyName("forceBorderlessFullscreen")]
    public bool ForceBorderlessFullscreen { get; set; } = true;

    [JsonPropertyName("progressUpdateIntervalMs")]
    public long ProgressUpdateIntervalMs { get; set; } = 1000;

    [JsonPropertyName("stalkerGammaRepo")]
    public string StalkerGammaRepo { get; set; } = "https://github.com/Grokitach/Stalker_GAMMA";

    [JsonPropertyName("gammaSetupRepo")]
    public string GammaSetupRepo { get; set; } = "https://github.com/Grokitach/gamma_setup";

    [JsonPropertyName("gammaLargeFilesRepo")]
    public string GammaLargeFilesRepo { get; set; } =
        "https://github.com/Grokitach/gamma_large_files_v2";

    [JsonPropertyName("teivazAnomalyGunslingerRepo")]
    public string TeivazAnomalyGunslingerRepo { get; set; } =
        "https://github.com/Grokitach/teivaz_anomaly_gunslinger";

    [JsonPropertyName("stalkerAddonApiUrl")]
    public string StalkerAddonApiUrl { get; set; } = "https://stalker-gamma.com/api/list";

    [JsonPropertyName("stalkerAnomalyModDbUrl")]
    public string StalkerAnomalyModDbUrl { get; set; } =
        "https://www.moddb.com/downloads/start/277404";

    [JsonPropertyName("stalkerAnomalyArchiveMd5")]
    public string StalkerAnomalyArchiveMd5 { get; set; } = "d6bce51a4e6d98f9610ef0aa967ba964";

    public async Task WriteAppSettingsAsync() =>
        await File.WriteAllTextAsync(
            "appsettings.json",
            JsonSerializer.Serialize(this, GlobalSettingsCtx.Default.GlobalSettings)
        );
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(GlobalSettings))]
public partial class GlobalSettingsCtx : JsonSerializerContext;
