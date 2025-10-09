using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.Tabs;
using stalker_gamma.core.ViewModels.Tabs.BackupTab;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;
using stalker_gamma.core.ViewModels.Tabs.MainTab;
using stalker_gamma.core.ViewModels.Tabs.ModListTab;
using ModDbUpdatesTabVm = stalker_gamma.core.ViewModels.Tabs.ModDbUpdatesTab.ModDbUpdatesTabVm;

namespace stalker_gamma.core.ViewModels.MainWindow;

public class MainWindowVm(
    MainTabVm mainTabVm,
    GammaUpdatesVm gammaUpdatesVm,
    ModDbUpdatesTabVm modDbUpdatesTabVm,
    ModListTabVm modListTabVm,
    BackupTabVm backupTabVm,
    IsBusyService isBusyService
) : ViewModelBase
{
    public MainTabVm MainTabVm { get; } = mainTabVm;
    public GammaUpdatesVm GammaUpdatesVm { get; } = gammaUpdatesVm;
    public ModDbUpdatesTabVm ModDbUpdatesTabVm { get; } = modDbUpdatesTabVm;
    public ModListTabVm ModListTabVm { get; } = modListTabVm;
    public BackupTabVm BackupTabVm { get; } = backupTabVm;
    public IsBusyService IsBusyService { get; } = isBusyService;
}
