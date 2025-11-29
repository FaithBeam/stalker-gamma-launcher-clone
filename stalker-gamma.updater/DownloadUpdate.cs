namespace stalker_gamma.updater;

public static class DownloadUpdate
{
    public sealed record Command(string DownloadLink, CancellationToken CancellationToken);

    public sealed class Handler(IHttpClientFactory hcf)
    {
        private readonly HttpClient _httpClient = hcf.CreateClient("githubDlArchive");

        public async Task<Stream> ExecuteAsync(Command c)
        {
            var response = await _httpClient.GetAsync(c.DownloadLink, c.CancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new DownloadUpdateException("Failed to get download release from Github");
            }

            return await response.Content.ReadAsStreamAsync(c.CancellationToken);
        }
    }
}

public class DownloadUpdateException(string msg) : Exception(msg);
