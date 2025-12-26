using stalker_gamma.core.Services.Enums;

namespace stalker_gamma.core.Models;

public record AddonRecord(
    int Index,
    string Name,
    string Url,
    string? MirrorUrl,
    string NiceUrl,
    string? Md5,
    string ArchiveDlPath,
    string ZipName,
    string ExtractDirectory,
    List<string> Instructions,
    AddonType AddonType,
    Action<string> OnStatus,
    Action<double> OnMd5Progress,
    Action<double> OnDlProgress,
    Action<double> OnExtractProgress
);
