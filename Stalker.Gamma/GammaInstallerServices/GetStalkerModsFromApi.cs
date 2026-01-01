using Stalker.Gamma.Models;

namespace Stalker.Gamma.GammaInstallerServices;

public interface IGetStalkerModsFromApi
{
    Task<string> GetModsAsync();
}

public class GetStalkerModsFromApi(StalkerGammaSettings settings, IHttpClientFactory hcf)
    : IGetStalkerModsFromApi
{
    private string ApiUrl => settings.ModpackMakerList;

    public async Task<string> GetModsAsync() => await _hc.GetStringAsync(ApiUrl);

    private readonly HttpClient _hc = hcf.CreateClient("stalkerApi");
}
