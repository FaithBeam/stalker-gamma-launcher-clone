using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using stalker_gamma.cli.Commands;
using stalker_gamma.cli.Services;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.tests.Services;

public class AnomalyInstallerServiceTests
{
    [OneTimeSetUp]
    public void StartTest()
    {
        Trace.Listeners.Add(new ConsoleTraceListener());
    }

    [OneTimeTearDown]
    public void EndTest()
    {
        Trace.Flush();
    }

    [Test]
    public async Task I_can_download_and_extract_Anomaly()
    {
        var cs = new CurlService();
        var ms = new MirrorService(cs);
        var moddb = new ModDb(cs, ms);
        var anomalyInstaller = new AnomalyInstaller(moddb);
        var sut = new FullInstallCmd(anomalyInstaller);

        await sut.FullInstall(
            "anomaly",
            "gamma",
            cacheDirectory: "cache",
            anomalyArchiveName: "anomaly.tar.zst"
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Directory.Exists("cache"));
            Assert.That(Directory.Exists("anomaly"));
            Assert.That(Directory.Exists("gamma"));
        }
    }
}
