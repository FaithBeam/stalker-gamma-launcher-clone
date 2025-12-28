namespace Stalker.Gamma.Models;

public interface IDownloadableRecord
{
    public string Name { get; }
    public string ArchiveName { get; }
    public Task DownloadAsync(CancellationToken cancellationToken);
    public Task ExtractAsync(CancellationToken cancellationToken);
}
