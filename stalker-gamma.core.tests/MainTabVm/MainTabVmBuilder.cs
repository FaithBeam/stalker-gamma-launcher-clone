using System;
using System.Net.Http;
using System.Reactive;
using NSubstitute;
using ReactiveUI;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.DowngradeModOrganizer;
using stalker_gamma.core.Services.GammaInstaller;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.Anomaly;
using stalker_gamma.core.Services.GammaInstaller.Mo2;
using stalker_gamma.core.Services.GammaInstaller.ModpackSpecific;
using stalker_gamma.core.Services.GammaInstaller.Shortcut;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;
using stalker_gamma.core.ViewModels.Tabs.MainTab;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Commands;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;
using stalker_gamma.core.ViewModels.Tabs.Queries;

namespace stalker_gamma.core.tests.MainTabVm;

internal class MainTabVmBuilder
{
    internal IOperatingSystemService OperatingSystemService { get; private set; } =
        new OperatingSystemService();

    internal IILongPathsStatusService? LongPathsStatusService { get; private set; }

    internal IIsRanWithWineService IsRanWithWineService { get; private set; } =
        Substitute.For<IIsRanWithWineService>();

    internal ICurlService CurlService { get; private set; } =
        new CurlService(Substitute.For<IHttpClientFactory>(), new OperatingSystemService());

    internal IIsBusyService IsBusyService { get; private set; } = new IsBusyService();

    internal IIsMo2VersionDowngraded IsMo2VersionDowngraded { get; private set; } =
        new IsMo2VersionDowngraded.Handler();

    private Action<IInteractionContext<string, Unit>> AppendLineInteractionHandler { get; set; } =
        context => context.SetOutput(Unit.Default);

    internal MainTabVmBuilder WithIsMo2VersionDowngraded(IIsMo2VersionDowngraded mo2Version)
    {
        IsMo2VersionDowngraded = mo2Version;
        return this;
    }

    internal MainTabVmBuilder WithOperatingSystemService(
        IOperatingSystemService operatingSystemService
    )
    {
        OperatingSystemService = operatingSystemService;
        return this;
    }

    internal MainTabVmBuilder WithLongPathsStatusService(
        IILongPathsStatusService longPathsStatusService
    )
    {
        LongPathsStatusService = longPathsStatusService;
        return this;
    }

    internal MainTabVmBuilder WithCurlService(ICurlService curlService)
    {
        CurlService = curlService;
        return this;
    }

    internal MainTabVmBuilder WithIsBusyService(IIsBusyService isBusyService)
    {
        IsBusyService = isBusyService;
        return this;
    }

    internal MainTabVmBuilder WithAppendLineInteractionHandler(
        Action<IInteractionContext<string, Unit>> handler
    )
    {
        AppendLineInteractionHandler = handler;
        return this;
    }

    internal MainTabVmBuilder WithIsRanWithWineService(IIsRanWithWineService svc)
    {
        IsRanWithWineService = svc;
        return this;
    }

    internal IMainTabVm Build()
    {
        var globalSettings = new GlobalSettings();
        var progressService = new ProgressService();
        var gitUtility = new GitUtility(progressService);
        var ranWithWine = IsRanWithWineService;
        var enableLongPathsOnWindows = new EnableLongPathsOnWindows.Handler();
        var addFoldersToWinDefenderExclusion = new AddFoldersToWinDefenderExclusion.Handler();
        var getAnomalyPathHandler = new GetAnomalyPath.Handler();
        var getGammaPathHandler = new GetGammaPath.Handler();
        var getGammaBackupFolderHandler = new GetGammaBackupFolder.Handler(globalSettings);
        var mirrorService = new MirrorService(CurlService);
        var modDb = new ModDb(progressService, CurlService, mirrorService);
        var modListFactory = new ModListRecordFactory(modDb, CurlService);
        var addonsAndSeparators = new AddonsAndSeparators(progressService, modListFactory);
        var modPackSpecific = new ModpackSpecific(progressService);
        var mo2 = new Mo2(progressService);
        var anomaly = new Anomaly(progressService);
        var shortcut = new Shortcut(progressService, OperatingSystemService);
        var gammaInstaller = new GammaInstaller(
            CurlService,
            progressService,
            gitUtility,
            addonsAndSeparators,
            modPackSpecific,
            mo2,
            anomaly,
            shortcut
        );
        var versionService = new VersionService();
        var downgradeModOrganizer = new DowngradeModOrganizer(progressService, versionService);
        var diffModsHandler = new DiffMods.Handler(CurlService, modListFactory);
        var getStalkerGammaLastCommitHandler = new GetStalkerGammaLastCommit.Handler(gitUtility);
        var getGitHubRepoCommitsHandler = new GetGitHubRepoCommits.Handler();
        var longPathsStatusSvc =
            LongPathsStatusService
            ?? new LongPathsStatus.Handler(IsRanWithWineService, OperatingSystemService);
        var vm = new ViewModels.Tabs.MainTab.MainTabVm(
            IsMo2VersionDowngraded,
            OperatingSystemService,
            longPathsStatusSvc,
            ranWithWine,
            enableLongPathsOnWindows,
            addFoldersToWinDefenderExclusion,
            getAnomalyPathHandler,
            getGammaPathHandler,
            getGammaBackupFolderHandler,
            CurlService,
            gammaInstaller,
            progressService,
            globalSettings,
            downgradeModOrganizer,
            versionService,
            IsBusyService,
            diffModsHandler,
            getStalkerGammaLastCommitHandler,
            getGitHubRepoCommitsHandler
        );
        vm.AppendLineInteraction.RegisterHandler(AppendLineInteractionHandler);
        return vm;
    }
}
