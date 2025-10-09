using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ReactiveUI;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Models;

namespace stalker_gamma_gui.Controls.Tabs;

public partial class GammaUpdatesTab : ReactiveUserControl<GammaUpdatesVm>
{
    public GammaUpdatesTab()
    {
        InitializeComponent();
        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                if (ViewModel is null)
                {
                    return;
                }

                ViewModel
                    .OpenGitDiffFileWindowInteraction.RegisterHandler(HandleOpenGitDiffFileWindow)
                    .DisposeWith(d);
            }
        );
    }

    private async Task HandleOpenGitDiffFileWindow(IInteractionContext<GitDiffWindowVm, Unit> obj)
    {
        var dlg = new DiffWindow { DataContext = obj.Input };
        await dlg.ShowDialog<Unit>((Window)TopLevel.GetTopLevel(this)!);
        obj.SetOutput(Unit.Default);
    }

    private void DiffGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e?.Source is null || ViewModel is null)
        {
            return;
        }

        if (e.Source is Control { DataContext: GitDiff tbGitDiff })
        {
            ViewModel.OpenGitDiffFileWindowCmd.Execute(tbGitDiff.Path).Subscribe();
        }
    }
}
