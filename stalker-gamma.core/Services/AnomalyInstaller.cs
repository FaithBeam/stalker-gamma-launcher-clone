namespace stalker_gamma.core.Services;

public class AnomalyInstaller(IHttpClientFactory hcf)
{
    public async Task DownloadAsync(string downloadPath, string extractPath) { }

    private const string AnomalyMdbUrl = "https://www.moddb.com/downloads/start/277404";
    private readonly HttpClient _hc = hcf.CreateClient();
}
