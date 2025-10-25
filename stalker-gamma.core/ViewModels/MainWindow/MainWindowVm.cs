using System.ComponentModel;
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

public class MainWindowVm(
    IMainTabVm mainTabVm,
    IGammaUpdatesVm gammaUpdatesVm,
    IModDbUpdatesTabVm modDbUpdatesTabVm,
    IModListTabVm modListTabVm,
    IBackupTabVm backupTabVm,
    IIsBusyService isBusyService
) : ViewModelBase, IMainWindowVm
{
    public IMainTabVm MainTabVm { get; } = mainTabVm;
    public IGammaUpdatesVm GammaUpdatesVm { get; } = gammaUpdatesVm;
    public IModDbUpdatesTabVm ModDbUpdatesTabVm { get; } = modDbUpdatesTabVm;
    public IModListTabVm ModListTabVm { get; } = modListTabVm;
    public IBackupTabVm BackupTabVm { get; } = backupTabVm;
    public IIsBusyService IsBusyService { get; } = isBusyService;
}
