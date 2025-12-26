using System.Collections.Frozen;
using stalker_gamma.core.Factories;
using stalker_gamma.core.Models;

namespace stalker_gamma.core.Services;

public class GetAddonsFromApiService(
    IHttpClientFactory hc,
    ModListRecordFactory modListRecordFactory,
    GlobalSettings globalSettings
)
{
    public async Task<FrozenDictionary<int, ModListRecord>> GetAddonsAsync(
        CancellationToken? cancellationToken = null
    )
    {
        cancellationToken ??= CancellationToken.None;
        return (
            await _hc.GetStringAsync(
                globalSettings.StalkerAddonApiUrl,
                cancellationToken: (CancellationToken)cancellationToken
            )
        )
            .Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((x, idx) => _modListRecordFactory.Create(x, idx))
            .Cast<ModListRecord>()
            .Select((x, i) => (x, i))
            .ToFrozenDictionary(x => x.i + 1, x => x.x);
    }

    private readonly HttpClient _hc = hc.CreateClient();
    private readonly ModListRecordFactory _modListRecordFactory = modListRecordFactory;
}
