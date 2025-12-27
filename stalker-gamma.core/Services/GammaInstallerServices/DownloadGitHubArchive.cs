using System.Buffers;

namespace stalker_gamma.core.Services.GammaInstallerServices;

public class DownloadGitHubArchive(IHttpClientFactory hcf)
{
    public async Task DownloadAsync(string url, string destination, Action<double> dlProgress)
    {
        const int bufferSize = 1024 * 1024;

        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            using var response = await _hc.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            await using var fs = new FileStream(
                destination,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: bufferSize
            );
            await using var contentStream = await response.Content.ReadAsStreamAsync();

            long totalBytesRead = 0;
            int bytesRead;

            while (
                (bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0
            )
            {
                await fs.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;

                if (totalBytes.HasValue)
                {
                    var progressPercentage = (double)totalBytesRead / totalBytes.Value;
                    dlProgress(progressPercentage);
                }
            }

            dlProgress(1);
        }
        catch (Exception e)
        {
            throw new DownloadGitHubArchiveException(
                $"""
                Error downloading archive
                Url: {url}
                Destination: {destination}
                {e}
                """,
                e
            );
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private readonly HttpClient _hc = hcf.CreateClient("githubDlArchive");
}

public class DownloadGitHubArchiveException(string msg, Exception innerException)
    : Exception(msg, innerException);
