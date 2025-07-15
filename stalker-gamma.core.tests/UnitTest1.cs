using stalker_gamma.core.Services.GammaInstaller;
using stalker_gamma.core.Services.GammaInstaller.Anomaly;
using stalker_gamma.core.Services.GammaInstaller.Mo2;

namespace stalker_gamma.core.tests;

public class Tests
{
    [Test]
    public async Task TestDownloadGammaData()
    {
        // await GammaInstaller.DownloadGammaData();
    }

    [Test]
    public async Task TestCheckGammaData()
    {
        // await GammaInstaller.CheckGammaData("", false);
    }

    [Test]
    public async Task TestOnInstallUpdateGammaAsync()
    {
        // await GammaInstaller.InstallUpdateGammaAsync(false, false, false, true, true, false);
    }

    [Test]
    public async Task TestPatchAnomaly()
    {
        // await Anomaly.Patch(
        //     "/Users/tech/RiderProjects/stalker-gamma-gui/stalker-gamma.core.tests/bin/Debug/net9.0",
        //     "/Users/tech/RiderProjects/stalker-gamma-gui/stalker-gamma.core.tests/bin/Debug/net9.0/G.A.M.M.A",
        //     "modpack_data/modlist.txt",
        //     true
        // );
    }

    [Test]
    public void TestMo2Setup()
    {
        // Mo2.Setup(
        //     @"C:\stalker-gamma-gui\stalker-gamma.core.tests\bin\Debug\net9.0",
        //     "G.A.M.M.A",
        //     @"C:\stalker-gamma-gui\stalker-gamma.core.tests\bin\Debug\net9.0\G.A.M.M.A",
        //     "modpack_data/modlist.txt"
        // );
    }
}
