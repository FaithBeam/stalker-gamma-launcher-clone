using System.Text.Json;
using Stalker.Gamma.Extensions;
using Stalker.Gamma.Models;

namespace Stalker.Gamma.Tests;

public class TestReadingModPackMakerJson
{
    private List<ModPackMakerRecord> ModsData { get; } =
        JsonSerializer.Deserialize<List<ModPackMakerRecord>>(
            File.ReadAllText("modpack_maker_list.json"),
            jsonTypeInfo: ModPackMakerCtx.Default.ListModPackMakerRecord
        )!;
    private List<ModPackMakerRecord> ModsDataModified { get; } =
        JsonSerializer.Deserialize<List<ModPackMakerRecord>>(
            File.ReadAllText("modpack_maker_list_modified.json"),
            jsonTypeInfo: ModPackMakerCtx.Default.ListModPackMakerRecord
        )!;

    [Test]
    public async Task TestReadingModPackMakerJsonAsync()
    {
        var diff = ModsData.Diff(ModsDataModified);
        ;
    }
}
