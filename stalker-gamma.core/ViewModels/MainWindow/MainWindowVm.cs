using stalker_gamma.core.ViewModels.Tabs;
using stalker_gamma.core.ViewModels.Tabs.ModsTab;

namespace stalker_gamma.core.ViewModels.MainWindow;

public class MainWindowVm(MainTabVm mainTabVm, ModsTabVm modsTabVm) : ViewModelBase
{
    public MainTabVm MainTabVm { get; } = mainTabVm;
    public ModsTabVm ModsTabVm { get; } = modsTabVm;
}
