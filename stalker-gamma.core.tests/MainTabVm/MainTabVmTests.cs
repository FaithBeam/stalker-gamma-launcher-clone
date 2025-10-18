using System;
using NSubstitute;

namespace stalker_gamma.core.tests.MainTabVm;

public class MainTabVmTests
{
    [Test]
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void AddFoldersToWinDefenderExclusionCmd_CanExecute_Tests(
        bool ranWithWineServiceReturns,
        bool expectedResult
    )
    {
        var mainTabBuilder = new MainTabVmBuilder();
        mainTabBuilder.IsRanWithWineService.IsRanWithWine().Returns(ranWithWineServiceReturns);
        var sut = mainTabBuilder.Build();

        sut.Activator.Activate();

        sut.AddFoldersToWinDefenderExclusionCmd.CanExecute.Subscribe(x =>
            Assert.That(x, Is.EqualTo(expectedResult))
        );
    }
}
