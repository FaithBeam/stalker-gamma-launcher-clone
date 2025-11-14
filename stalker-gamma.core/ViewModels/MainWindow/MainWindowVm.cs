using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using ReactiveUI;
using stalker_gamma.core.Services;
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
        SwitchToMyFork.SwitchToMyFork.Handler switchToMyForkHandler
    )
    {
        Activator = new ViewModelActivator();
        MainTabVm = mainTabVm;
        GammaUpdatesVm = gammaUpdatesVm;
        ModDbUpdatesTabVm = modDbUpdatesTabVm;
        ModListTabVm = modListTabVm;
        BackupTabVm = backupTabVm;
        IsBusyService = isBusyService;

        SwitchToMyForkCmd = ReactiveCommand.CreateFromTask(async () =>
            await switchToMyForkHandler.ExecuteAsync()
        );
        SwitchToMyForkCmd.ThrownExceptions.Subscribe(x => Trace.WriteLine(x.ToString()));
    }

    public IMainTabVm MainTabVm { get; }
    public IGammaUpdatesVm GammaUpdatesVm { get; }
    public IModDbUpdatesTabVm ModDbUpdatesTabVm { get; }
    public IModListTabVm ModListTabVm { get; }
    public IBackupTabVm BackupTabVm { get; }
    public IIsBusyService IsBusyService { get; }
    public ViewModelActivator Activator { get; }
    private ReactiveCommand<Unit, Unit> SwitchToMyForkCmd { get; }
}
