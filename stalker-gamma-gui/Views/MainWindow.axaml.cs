using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using ReactiveUI;
using stalker_gamma_gui.Controls.Dialogs;
using stalker_gamma_gui.ViewModels.Dialogs;
using stalker_gamma_gui.ViewModels.MainWindow;

namespace stalker_gamma_gui.Views;

public partial class MainWindow : ReactiveWindow<MainWindowVm>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                if (ViewModel is null)
                {
                    return;
                }

                ViewModel.ShowUpdateDialogInteraction.RegisterHandler(ShowUpdateDialog);
            }
        );
    }

    private async Task ShowUpdateDialog(
        IInteractionContext<UpdateLauncherDialogVm, DoUpdateCmdParam> ctx
    )
    {
        var dlg = new UpdateDialog { DataContext = ctx.Input };
        var result = await dlg.ShowDialog<DoUpdateCmdParam>(this);
        ctx.SetOutput(result);
    }
}
