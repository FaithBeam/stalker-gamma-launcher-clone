using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.Tabs;
using stalker_gamma.core.ViewModels.Tabs.ModsTab;

namespace stalker_gamma.core.ViewModels.MainWindow;

public class MainWindowVm(MainTabVm mainTabVm, ModsTabVm modsTabVm, IsBusyService isBusyService)
    : ViewModelBase
{
    public MainTabVm MainTabVm { get; } = mainTabVm;
    public ModsTabVm ModsTabVm { get; } = modsTabVm;
    public IsBusyService IsBusyService { get; } = isBusyService;
}
