namespace stalker_gamma.core.Services.GammaInstallerServices;

public class DownloadAndExtractRecordService
{
    public required string RepoPath { get; set; }
    public required string DestinationDir { get; set; }
    public required string RepoUrl { get; set; }
    public required Action<double> OnDlProgress { get; set; } = _ => { };
    public required Action<double> OnExtractProgress { get; set; } = _ => { };
    public required Func<bool, Task> DownloadFunc { get; set; }
    public required Func<Task> ExtractFunc { get; set; }
    public Action OnComplete { get; set; } = () => { };
    public Task? ExtractPrereqTask { get; set; }
    public CancellationToken? CancellationToken { get; set; }

    public async Task DownloadAndExtractAsync(bool invalidateMirror = false)
    {
        await DownloadFunc(invalidateMirror);

        if (ExtractPrereqTask is not null)
        {
            await ExtractPrereqTask.WaitAsync(
                CancellationToken ?? System.Threading.CancellationToken.None
            );
        }

        await ExtractFunc();

        OnComplete();
    }
}
