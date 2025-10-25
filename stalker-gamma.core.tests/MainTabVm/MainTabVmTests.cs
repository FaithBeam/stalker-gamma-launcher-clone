using System;
using System.Reactive.Linq;
using NSubstitute;
using stalker_gamma.core.Services;

namespace stalker_gamma.core.tests.MainTabVm;

public class MainTabVmTests
{
    [Test]
    [TestCase(false, true, false, true)]
    [TestCase(false, true, null, false)]
    [TestCase(true, true, null, false)]
    [TestCase(true, true, false, false)]
    [TestCase(true, true, true, false)]
    [TestCase(true, false, true, false)]
    [TestCase(false, false, true, false)]
    [TestCase(false, false, false, false)]
    [TestCase(true, false, false, false)]
    public void EnableLongPaths_CanExecute(
        bool ranWithWineServiceReturns,
        bool isWindows,
        bool? longPathsStatusSvcReturns,
        bool expectedResult
    )
    {
        var operatingSystemService = Substitute.For<IOperatingSystemService>();
        operatingSystemService.IsWindows().Returns(isWindows);
        var longPathsStatusSvc = Substitute.For<IILongPathsStatusService>();
        longPathsStatusSvc.Execute().Returns(longPathsStatusSvcReturns);
        var mainTabBuilder = new MainTabVmBuilder()
            .WithOperatingSystemService(operatingSystemService)
            .WithLongPathsStatusService(longPathsStatusSvc);
        mainTabBuilder.IsRanWithWineService.IsRanWithWine().Returns(ranWithWineServiceReturns);
        var sut = mainTabBuilder.Build();

        sut.Activator.Activate();

        sut.EnableLongPathsOnWindowsCmd.CanExecute.Subscribe(x =>
            Assert.That(x, Is.EqualTo(expectedResult))
        );
    }

    [Test]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    public void AddFoldersToWinDefenderExclusionCmd_CanExecute_Tests(
        bool ranWithWineServiceReturns,
        bool isWindowsReturns,
        bool expectedResult
    )
    {
        var operatingSystemService = Substitute.For<IOperatingSystemService>();
        operatingSystemService.IsWindows().Returns(isWindowsReturns);
        var mainTabBuilder = new MainTabVmBuilder().WithOperatingSystemService(
            operatingSystemService
        );
        mainTabBuilder.IsRanWithWineService.IsRanWithWine().Returns(ranWithWineServiceReturns);
        var sut = mainTabBuilder.Build();

        sut.Activator.Activate();

        sut.AddFoldersToWinDefenderExclusionCmd.CanExecute.Subscribe(x =>
            Assert.That(x, Is.EqualTo(expectedResult))
        );
    }

    // [Test]
    // [TestCase(false, true, true, true, true, true)]
    // [TestCase(true, true, true, true, true, false)]
    // [TestCase(false, false, true, true, true, false)]
    // [TestCase(false, true, false, true, true, false)]
    // [TestCase(false, true, true, true, false, true)]
    // [TestCase(false, true, true, false, true, false)]
    // public void InstallUpdateGammaCmd_CanExecute(
    //     bool isBusy,
    //     bool inGrokModDir,
    //     bool curlReady,
    //     bool? longPathsStatus,
    //     bool isWindows,
    //     bool expected
    // )
    // {
    //     var isBusySvc = Substitute.For<IIsBusyService>();
    //     isBusySvc.IsBusy.Returns(isBusy);
    //     var curlSvc = Substitute.For<ICurlService>();
    //     curlSvc.Ready.Returns(curlReady);
    //     var longPathsStatusSvc = Substitute.For<IILongPathsStatusService>();
    //     longPathsStatusSvc.Execute().Returns(longPathsStatus);
    //     var operatingSystemSvc = Substitute.For<IOperatingSystemService>();
    //     operatingSystemSvc.IsWindows().Returns(isWindows);
    //     var mainTabBuilder = new MainTabVmBuilder()
    //         .WithIsBusyService(isBusySvc)
    //         .WithCurlService(curlSvc)
    //         .WithLongPathsStatusService(longPathsStatusSvc)
    //         .WithOperatingSystemService(operatingSystemSvc);
    //     var sut = mainTabBuilder.Build();
    //
    //     sut.Activator.Activate();
    //     sut.InGrokModDir = inGrokModDir;
    //
    //     sut.InstallUpdateGamma.CanExecute.Subscribe(x => Assert.That(x, Is.EqualTo(expected)));
    // }
}
