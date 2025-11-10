using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.Dialogs;
using stalker_gamma.core.ViewModels.MainWindow.Factories;
using stalker_gamma.core.ViewModels.MainWindow.Queries;
using stalker_gamma.core.ViewModels.Tabs;
using stalker_gamma.core.ViewModels.Tabs.BackupTab;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;
using stalker_gamma.core.ViewModels.Tabs.MainTab;
using stalker_gamma.core.ViewModels.Tabs.ModDbUpdatesTab;
using stalker_gamma.core.ViewModels.Tabs.ModListTab;
using ModDbUpdatesTabVm = stalker_gamma.core.ViewModels.Tabs.ModDbUpdatesTab.ModDbUpdatesTabVm;

namespace stalker_gamma.core.ViewModels.MainWindow;

public interface IMainWindowVm
{
    IMainTabVm MainTabVm { get; }
    IGammaUpdatesVm GammaUpdatesVm { get; }
    IModDbUpdatesTabVm ModDbUpdatesTabVm { get; }
    IModListTabVm ModListTabVm { get; }
    IBackupTabVm BackupTabVm { get; }
    IIsBusyService IsBusyService { get; }
    IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changing { get; }
    IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changed { get; }
    IObservable<Exception> ThrownExceptions { get; }
    IDisposable SuppressChangeNotifications();
    bool AreChangeNotificationsEnabled();
    IDisposable DelayChangeNotifications();
    event PropertyChangingEventHandler? PropertyChanging;
    event PropertyChangedEventHandler? PropertyChanged;
}

public class MainWindowVm : ViewModelBase, IMainWindowVm, IActivatableViewModel
{
    public MainWindowVm(
        IMainTabVm mainTabVm,
        IGammaUpdatesVm gammaUpdatesVm,
        IModDbUpdatesTabVm modDbUpdatesTabVm,
        IModListTabVm modListTabVm,
        IBackupTabVm backupTabVm,
        IIsBusyService isBusyService,
        UpdateAvailable.Handler updateAvailableHandler,
        UpdateLauncherDialogVmFactory updateLauncherDialogVmFactory
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

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                UpdateAvailableCmd.Execute().Subscribe();
            }
        );
    }

    public IMainTabVm MainTabVm { get; }
    public IGammaUpdatesVm GammaUpdatesVm { get; }
    public IModDbUpdatesTabVm ModDbUpdatesTabVm { get; }
    public IModListTabVm ModListTabVm { get; }
    public IBackupTabVm BackupTabVm { get; }
    public IIsBusyService IsBusyService { get; }
    public ViewModelActivator Activator { get; }
    private ReactiveCommand<Unit, UpdateAvailable.Response> UpdateAvailableCmd { get; }
    public ReactiveCommand<UpdateLauncherDialogVm, DoUpdateCmdParam> ShowUpdateDialogCmd { get; }
    public IInteraction<
        UpdateLauncherDialogVm,
        DoUpdateCmdParam
    > ShowUpdateDialogInteraction { get; }
}
