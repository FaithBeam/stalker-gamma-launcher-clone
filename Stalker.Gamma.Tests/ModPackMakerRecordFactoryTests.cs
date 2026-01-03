using Stalker.Gamma.Factories;

namespace Stalker.Gamma.Tests;

public class ModPackMakerRecordFactoryTests
{
    private string ModsData { get; } = File.ReadAllText("modpack_maker_list.txt");

    [Test]
    public void TestParseModsData()
    {
        var sut = new ModPackMakerRecordFactory();
        var records = sut.Create(ModsData);

        Assert.That(records, Has.Count.EqualTo(459));
    }
}
