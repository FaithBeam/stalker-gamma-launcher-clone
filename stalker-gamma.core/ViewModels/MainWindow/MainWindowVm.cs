using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.Dialogs;
using stalker_gamma.core.ViewModels.MainWindow.Factories;
using stalker_gamma.core.ViewModels.MainWindow.Queries;
using stalker_gamma.core.ViewModels.Services;
using stalker_gamma.core.ViewModels.Tabs.BackupTab;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;
using stalker_gamma.core.ViewModels.Tabs.MainTab;
using stalker_gamma.core.ViewModels.Tabs.ModDbUpdatesTab;
using stalker_gamma.core.ViewModels.Tabs.ModListTab;

namespace stalker_gamma.core.ViewModels.MainWindow;

public class MainWindowVm : ViewModelBase, IActivatableViewModel
{
    public MainWindowVm(
        MainTabVm mainTabVm,
        IGammaUpdatesVm gammaUpdatesVm,
        IModDbUpdatesTabVm modDbUpdatesTabVm,
        IModListTabVm modListTabVm,
        IBackupTabVm backupTabVm,
        IIsBusyService isBusyService,
        ModalService modalService,
        UpdateAvailable.Handler updateAvailableHandler,
        UpdateLauncherDialogVmFactory updateLauncherDialogVmFactory,
        GlobalSettings globalSettings
    )
    {
        ShowUpdateDialogInteraction = new Interaction<UpdateLauncherDialogVm, DoUpdateCmdParam>();
        ShowUpdateDialogCmd = ReactiveCommand.CreateFromTask<
            UpdateLauncherDialogVm,
            DoUpdateCmdParam
        >(async vm => await ShowUpdateDialogInteraction.Handle(vm));
        UpdateAvailableCmd = ReactiveCommand.CreateFromTask(async _ =>
            await updateAvailableHandler.ExecuteAsync()
        );
        UpdateAvailableCmd
            .Where(updateAvailable => updateAvailable.IsUpdateAvailable)
            .Subscribe(info =>
                ShowUpdateDialogCmd.Execute(updateLauncherDialogVmFactory.Create(info)).Subscribe()
            );
        Activator = new ViewModelActivator();
        MainTabVm = mainTabVm;
        GammaUpdatesVm = gammaUpdatesVm;
        ModDbUpdatesTabVm = modDbUpdatesTabVm;
        ModListTabVm = modListTabVm;
        BackupTabVm = backupTabVm;
        IsBusyService = isBusyService;
        ModalService = modalService;

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                if (globalSettings.CheckForLauncherUpdates)
                {
                    UpdateAvailableCmd.Execute().Subscribe().DisposeWith(d);
                }
            }
        );
    }

    public MainTabVm MainTabVm { get; }
    public IGammaUpdatesVm GammaUpdatesVm { get; }
    public IModDbUpdatesTabVm ModDbUpdatesTabVm { get; }
    public IModListTabVm ModListTabVm { get; }
    public IBackupTabVm BackupTabVm { get; }
    public IIsBusyService IsBusyService { get; }
    public ModalService ModalService { get; }
    public ViewModelActivator Activator { get; }
    private ReactiveCommand<Unit, UpdateAvailable.Response> UpdateAvailableCmd { get; }
    public ReactiveCommand<UpdateLauncherDialogVm, DoUpdateCmdParam> ShowUpdateDialogCmd { get; }
    public IInteraction<
        UpdateLauncherDialogVm,
        DoUpdateCmdParam
    > ShowUpdateDialogInteraction { get; }
}
