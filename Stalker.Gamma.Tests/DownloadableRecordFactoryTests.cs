using NSubstitute;
using Stalker.Gamma.Factories;
using Stalker.Gamma.GammaInstallerServices;
using Stalker.Gamma.Models;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.Tests;

public class DownloadableRecordFactoryTests
{
    private string ModsData { get; } = File.ReadAllText("mods.txt");

    [Test]
    public void TestParseModsData()
    {
        var modListRecordFactory = new ModPackMakerRecordFactory();
        var records = modListRecordFactory.Create(ModsData);
        var hcf = Substitute.For<IHttpClientFactory>();
        hcf.CreateClient(Arg.Any<string>()).Returns(new HttpClient());
        var gammaProgress = Substitute.For<GammaProgress>();
        var stalkerGammaSettings = new StalkerGammaSettings();
        // var sut = new DownloadableRecordFactory(stalkerGammaSettings, hcf, gammaProgress);
        // var gammaDir = "gamma";
        //
        // var downloadableRecords = records
        //     .Select((rec, idx) => sut.TryCreate(idx, gammaDir, rec, out var dlRec) ? dlRec : null)
        //     .Where(x => x != null)
        //     .Select(x => x!)
        //     .ToList();
        ;
    }

    [Test]
    public void TestCreateSpecialGitRepos()
    {
        var hcf = Substitute.For<IHttpClientFactory>();
        hcf.CreateClient(Arg.Any<string>()).Returns(new HttpClient());
        var gammaProgress = Substitute.For<GammaProgress>();
        var stalkerGammaSettings = new StalkerGammaSettings();
        // var sut = new DownloadableRecordFactory(stalkerGammaSettings, hcf, gammaProgress);
        //
        // var gammaSetup = sut.CreateGammaSetupRecord("gamma", "anomaly");
        // var gammaLargeFiles = sut.CreateGammaLargeFilesRecord("gamma");
        // var stalkerGamma = sut.CreateStalkerGammaRecord("gamma", "anomaly");
        // var teivazAnomalyGunslinger = sut.CreateTeivazAnomalyGunslingerRecord("gamma");
        // var anomalyInstaller = sut.CreateAnomalyRecord("gamma", "anomaly");
        ;
    }

    [Test]
    public void TestCreateGroupedDownloadableRecords()
    {
        var modListRecordFactory = new ModPackMakerRecordFactory();
        var records = modListRecordFactory.Create(ModsData);
        var hcf = Substitute.For<IHttpClientFactory>();
        hcf.CreateClient(Arg.Any<string>()).Returns(new HttpClient());
        var gammaProgress = Substitute.For<GammaProgress>();
        var stalkerGammaSettings = new StalkerGammaSettings();
        // var sut = new DownloadableRecordFactory(stalkerGammaSettings, hcf, gammaProgress);
        // var gammaDir = "gamma";
        // var downloadableRecords = records
        //     .Select((rec, idx) => sut.TryCreate(idx, gammaDir, rec, out var dlRec) ? dlRec : null)
        //     .Where(x => x != null)
        //     .Select(x => x!)
        //     .ToList();
        //
        // var grouped = sut.CreateGroupedDownloadableRecords(downloadableRecords);
        //
        // Assert.That(grouped, Has.Count.EqualTo(380));
    }
}
