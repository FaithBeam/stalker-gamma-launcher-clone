using System.Reactive.Disposables;
using ReactiveUI;

namespace stalker_gamma.core.ViewModels.Dialogs.DownloadProgress;

public class DownloadProgressVm : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; }

    public DownloadProgressVm()
    {
        Activator = new ViewModelActivator();

        this.WhenActivated((CompositeDisposable d) => { });
    }
}
