using Stalker.Gamma.Models;

namespace Stalker.Gamma.GammaInstallerServices;

public interface IGetStalkerModsFromApi
{
    Task<string> GetModsAsync();
}

public class GetStalkerModsFromApi(StalkerGammaSettings settings, IHttpClientFactory hcf)
    : IGetStalkerModsFromApi
{
    private readonly string _apiUrl = settings.ListStalkerModsUrl;

    public async Task<string> GetModsAsync() => await _hc.GetStringAsync(_apiUrl);

    private readonly HttpClient _hc = hcf.CreateClient("stalkerApi");
}
