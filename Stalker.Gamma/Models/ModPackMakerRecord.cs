namespace Stalker.Gamma.Models;

public class ModPackMakerRecord
{
    public required string DlLink { get; init; }
    public string? Instructions { get; init; }
    public string? Patch { get; init; }
    public string? AddonName { get; init; }
    public string? ModDbUrl { get; init; }
    public string? ZipName { get; init; }
    public string? Md5ModDb { get; init; }
}
