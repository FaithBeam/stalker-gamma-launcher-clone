using stalker_gamma.core.ViewModels.Tabs.BackupTab.Enums;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Queries;

public static class GetEstimate
{
    public sealed record Query(
        Compressor Compressor,
        CompressionLevel CompressionLevel,
        BackupType BackupType
    );

    public sealed class Handler
    {
        public string Execute(Query q) =>
            q.BackupType switch
            {
                BackupType.Partial => q.Compressor switch
                {
                    Compressor.Lzma2 => q.CompressionLevel switch
                    {
                        CompressionLevel.None => "≈ 6 minutes, 28gb 8c/16t CPU",
                        CompressionLevel.Fast => "≈ 5 minutes, 28gb 8c/16t CPU",
                        CompressionLevel.Ultra => "",
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(q.CompressionLevel),
                            q.CompressionLevel,
                            null
                        ),
                    },
                    Compressor.Zstd => q.CompressionLevel switch
                    {
                        CompressionLevel.None => "not used",
                        CompressionLevel.Fast => "≈ 3 minute, 34gb 8c/16t CPU",
                        CompressionLevel.Ultra => "not used",
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(q.CompressionLevel),
                            q.CompressionLevel,
                            null
                        ),
                    },
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(q.Compressor),
                        q.Compressor,
                        null
                    ),
                },
                BackupType.Full => q.Compressor switch
                {
                    Compressor.Lzma2 => q.CompressionLevel switch
                    {
                        CompressionLevel.None => "CHANGEME",
                        CompressionLevel.Fast => "≈ 15 minutes, 54gb 8c/16t CPU",
                        CompressionLevel.Ultra => "≈ 50 minutes, 50gb 8c/16t CPU",
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(q.CompressionLevel),
                            q.CompressionLevel,
                            null
                        ),
                    },
                    Compressor.Zstd => q.CompressionLevel switch
                    {
                        CompressionLevel.None => "changeme",
                        CompressionLevel.Fast => "≈ 10 minutes, 65gb 8c/16t CPU",
                        CompressionLevel.Ultra => "≈ 135 minutes, 42gb 8c/16t CPU",
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(q.CompressionLevel),
                            q.CompressionLevel,
                            null
                        ),
                    },
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(q.Compressor),
                        q.Compressor,
                        null
                    ),
                },
                _ => throw new ArgumentOutOfRangeException(
                    nameof(q.BackupType),
                    q.BackupType,
                    null
                ),
            };
    }
}
