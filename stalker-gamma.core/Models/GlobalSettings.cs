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
