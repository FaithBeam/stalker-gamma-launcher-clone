using System.Text.Json.Serialization;

namespace stalker_gamma.cli.Models;

public class CliProfile
{
    public bool Active { get; set; }
    public string ProfileName { get; set; } = "Gamma";
    public string Anomaly { get; set; } = Path.Join("gamma", "anomaly");
    public string Gamma { get; set; } = Path.Join("gamma", "gamma");
    public string Cache { get; set; } = Path.Join("gamma", "cache");
    public string Mo2Profile { get; set; } = "G.A.M.M.A";
    public int DownloadThreads { get; set; } = 2;
    public string ModPackMakerUrl { get; set; } = "https://stalker-gamma.com/api/list";
    public string ModListUrl { get; set; } =
        "https://raw.githubusercontent.com/Grokitach/Stalker_GAMMA/refs/heads/main/G.A.M.M.A/modpack_data/modlist.txt";
}

[JsonSerializable(typeof(CliProfile))]
public partial class CliProfileCtx : JsonSerializerContext;
