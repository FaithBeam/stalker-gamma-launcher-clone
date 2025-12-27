using System.IO;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace stalker_gamma.core.Models;

public class SettingsFile : ReactiveObject
{
    /// <summary>
    /// This directory will contain folders anomaly, gamma, and cache
    /// </summary>
    public string? BaseGammaDirectory
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    [JsonIgnore]
    public string? AnomalyDir =>
        string.IsNullOrWhiteSpace(BaseGammaDirectory)
            ? null
            : Path.Combine(BaseGammaDirectory, "anomaly");

    [JsonIgnore]
    public string? GammaDir =>
        string.IsNullOrWhiteSpace(BaseGammaDirectory)
            ? null
            : Path.Combine(BaseGammaDirectory, "gamma");

    [JsonIgnore]
    public string? CacheDir =>
        string.IsNullOrWhiteSpace(BaseGammaDirectory)
            ? null
            : Path.Combine(BaseGammaDirectory, "cache");
}

[JsonSerializable(typeof(SettingsFile))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class SettingsFileCtx : JsonSerializerContext;
