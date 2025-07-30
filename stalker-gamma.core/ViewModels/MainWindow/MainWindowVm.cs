using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.Tabs;
using ModDbUpdatesTabVm = stalker_gamma.core.ViewModels.Tabs.ModDbUpdatesTab.ModDbUpdatesTabVm;

namespace stalker_gamma.core.ViewModels.MainWindow;

public class MainWindowVm(
    MainTabVm mainTabVm,
    ModDbUpdatesTabVm modDbUpdatesTabVm,
    IsBusyService isBusyService
) : ViewModelBase
{
    public MainTabVm MainTabVm { get; } = mainTabVm;
    public ModDbUpdatesTabVm ModDbUpdatesTabVm { get; } = modDbUpdatesTabVm;
    public IsBusyService IsBusyService { get; } = isBusyService;
}
