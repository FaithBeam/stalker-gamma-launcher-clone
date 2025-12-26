using stalker_gamma_gui.ViewModels.Dialogs;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;

namespace stalker_gamma_gui.ViewModels.MainWindow.Factories;

public class UpdateLauncherDialogVmFactory(GlobalSettings gs)
{
    public UpdateLauncherDialogVm Create(UpdateAvailable.Response info) => new(info, gs);
}
