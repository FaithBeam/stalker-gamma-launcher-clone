using stalker_gamma.core.Models;
using stalker_gamma.core.ViewModels.Dialogs;
using stalker_gamma.core.ViewModels.MainWindow.Queries;

namespace stalker_gamma.core.ViewModels.MainWindow.Factories;

public class UpdateLauncherDialogVmFactory(GlobalSettings gs)
{
    public UpdateLauncherDialogVm Create(UpdateAvailable.Response info) => new(info, gs);
}
