using Stalker.Gamma.Factories;

namespace Stalker.Gamma.Tests;

public class SeparatorsFactoryTest
{
    private string ModsData { get; } = File.ReadAllText("mods.txt");

    [Test]
    public void TestCreateSeparators()
    {
        var modListRecordFactory = new ModPackMakerRecordFactory();
        var records = modListRecordFactory.Create(ModsData);
        var sut = new SeparatorsFactory();

        var separators = sut.Create(records);

        Assert.That(separators, Has.Count.EqualTo(19));
    }
}
