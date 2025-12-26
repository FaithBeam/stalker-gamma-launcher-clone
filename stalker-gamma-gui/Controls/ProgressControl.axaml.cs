using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;
using stalker_gamma_gui.ViewModels.Tabs.MainTab;

namespace stalker_gamma_gui.Controls;

public partial class ProgressControl : ReactiveUserControl<ModDownloadExtractProgressVm>
{
    public ProgressControl()
    {
        InitializeComponent();

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                if (ViewModel is null)
                {
                    return;
                }
            }
        );
    }
}
