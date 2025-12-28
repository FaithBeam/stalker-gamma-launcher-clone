using Stalker.Gamma.Factories;

namespace Stalker.Gamma.Tests;

public class ModListRecordFactoryTests
{
    private string ModsData { get; } = File.ReadAllText("mods.txt");

    [Test]
    public void TestParseModsData()
    {
        var sut = new ModListRecordFactory();
        var records = sut.Create(ModsData);

        Assert.That(records, Has.Count.EqualTo(459));
    }
}
