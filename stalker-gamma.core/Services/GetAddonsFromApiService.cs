using System.Collections.Frozen;
using stalker_gamma.core.Factories;
using stalker_gamma.core.Models;

namespace stalker_gamma.core.Services;

public static class GetAddonsFromApiService
{
    public static FrozenDictionary<int, ModListRecord> GetAddons(string modList) =>
        modList
            .Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ModListRecordFactory.Create)
            .Cast<ModListRecord>()
            .Select((x, i) => (x, i))
            .ToFrozenDictionary(x => x.i + 1, x => x.x);
}
