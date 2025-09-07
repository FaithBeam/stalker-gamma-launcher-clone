using System;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using stalker_gamma.core.ViewModels.Tabs.BackupTab;

namespace stalker_gamma_gui.Controls.Tabs;

public partial class BackupTab : ReactiveUserControl<BackupTabVm>
{
    public BackupTab()
    {
        InitializeComponent();
        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                if (ViewModel is null)
                {
                    return;
                }

                ViewModel.AppendLineInteraction.RegisterHandler(AppendLineHandler).DisposeWith(d);
            }
        );
    }

    private void AppendLineHandler(IInteractionContext<string, Unit> interactionCtx)
    {
        BackupTxt.AppendText($"{DateTime.Now:g}:\t{interactionCtx.Input}");
        BackupTxt.AppendText(Environment.NewLine);
        BackupTxt.ScrollToLine(BackupTxt.LineCount);
        interactionCtx.SetOutput(Unit.Default);
    }
}
