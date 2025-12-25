using System.Text.Json.Serialization;

namespace stalker_gamma.core.Models;

public class SettingsFile
{
    /// <summary>
    /// This directory will contain folders anomaly, gamma, and cache
    /// </summary>
    public string? BaseGammaDirectory { get; set; }
    [JsonIgnore]
    public string? AnomalyDir => string.IsNullOrWhiteSpace(BaseGammaDirectory) ? null : Path.Combine(BaseGammaDirectory, "anomaly");
    [JsonIgnore]
    public string? GammaDir => string.IsNullOrWhiteSpace(BaseGammaDirectory) ? null : Path.Combine(BaseGammaDirectory, "gamma");
    [JsonIgnore]
    public string? CacheDir => string.IsNullOrWhiteSpace(BaseGammaDirectory) ? null : Path.Combine(BaseGammaDirectory, "cache");
}

[JsonSerializable(typeof(SettingsFile))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class SettingsFileCtx : JsonSerializerContext;
