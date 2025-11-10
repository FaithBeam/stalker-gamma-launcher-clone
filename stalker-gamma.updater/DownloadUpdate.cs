namespace stalker_gamma.updater;

public static class DownloadUpdate
{
    public sealed record Command(
        string DownloadLink,
        string DestinationPath,
        IProgress<double> Progress,
        CancellationToken CancellationToken
    );

    public sealed class Handler(IHttpClientFactory hcf)
    {
        private readonly HttpClient _httpClient = hcf.CreateClient("githubDlArchive");

        public async Task ExecuteAsync(Command c)
        {
            using var response = await _httpClient.GetAsync(c.DownloadLink, c.CancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new DownloadUpdateException("Failed to get download release from Github");
            }

            var totalBytes =
                response.Content.Headers.ContentLength
                ?? throw new DownloadUpdateException("Unable to get content length");
            await using var contentStream = await response.Content.ReadAsStreamAsync(
                c.CancellationToken
            );
            await using var fileStream = File.OpenWrite(c.DestinationPath);

            var buffer = new byte[8192];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, c.CancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), c.CancellationToken);
                totalBytesRead += bytesRead;
                c.Progress.Report(totalBytesRead / (double)totalBytes);
            }
        }
    }
}

public class DownloadUpdateException(string msg) : Exception(msg);
