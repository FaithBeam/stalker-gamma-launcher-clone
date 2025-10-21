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
    internal IIsRanWithWineService IsRanWithWineService { get; private set; } =
        Substitute.For<IIsRanWithWineService>();

    private Action<IInteractionContext<string, Unit>> AppendLineInteractionHandler { get; set; } =
        context => context.SetOutput(Unit.Default);

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
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var progressService = new ProgressService();
        var gitUtility = new GitUtility(progressService);
        var ranWithWine = IsRanWithWineService;
        var enableLongPathsOnWindows = new EnableLongPathsOnWindows.Handler();
        var addFoldersToWinDefenderExclusion = new AddFoldersToWinDefenderExclusion.Handler();
        var getAnomalyPathHandler = new GetAnomalyPath.Handler();
        var getGammaPathHandler = new GetGammaPath.Handler();
        var getGammaBackupFolderHandler = new GetGammaBackupFolder.Handler(globalSettings);
        var curlService = new CurlService(httpClientFactory);
        var mirrorService = new MirrorService(curlService);
        var modDb = new ModDb(progressService, curlService, mirrorService);
        var modListFactory = new ModListRecordFactory(modDb, curlService);
        var addonsAndSeparators = new AddonsAndSeparators(progressService, modListFactory);
        var modPackSpecific = new ModpackSpecific(progressService);
        var mo2 = new Mo2(progressService);
        var anomaly = new Anomaly(progressService);
        var shortcut = new Shortcut(progressService);
        var gammaInstaller = new GammaInstaller(
            curlService,
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
        var isBusyService = new IsBusyService();
        var diffModsHandler = new DiffMods.Handler(curlService, modListFactory);
        var getStalkerGammaLastCommitHandler = new GetStalkerGammaLastCommit.Handler(gitUtility);
        var getGitHubRepoCommitsHandler = new GetGitHubRepoCommits.Handler();
        var vm = new ViewModels.Tabs.MainTab.MainTabVm(
            ranWithWine,
            enableLongPathsOnWindows,
            addFoldersToWinDefenderExclusion,
            getAnomalyPathHandler,
            getGammaPathHandler,
            getGammaBackupFolderHandler,
            curlService,
            gammaInstaller,
            progressService,
            globalSettings,
            downgradeModOrganizer,
            versionService,
            isBusyService,
            diffModsHandler,
            getStalkerGammaLastCommitHandler,
            getGitHubRepoCommitsHandler
        );
        vm.AppendLineInteraction.RegisterHandler(AppendLineInteractionHandler);
        return vm;
    }
}
