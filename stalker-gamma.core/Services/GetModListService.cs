namespace stalker_gamma.core.Services;

public class GetModListService(IHttpClientFactory hcf)
{
    public async Task<string> ExecuteAsync(string url, CancellationToken cancellationToken) =>
        await _httpClient.GetStringAsync(url, cancellationToken);

    private readonly HttpClient _httpClient = hcf.CreateClient();
}
