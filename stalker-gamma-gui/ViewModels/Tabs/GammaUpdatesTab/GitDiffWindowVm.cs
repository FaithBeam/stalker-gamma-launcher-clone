using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using stalker_gamma_gui.ViewModels.Services;
using stalker_gamma_gui.ViewModels.Tabs.GammaUpdatesTab.Queries;

namespace stalker_gamma_gui.ViewModels.Tabs.GammaUpdatesTab;

public class GitDiffWindowVm : ViewModelBase, IActivatableViewModel
{
    public GitDiffWindowVm(
        GetGitDiffFile.Handler gitDiffFileHandler,
        string dir,
        string filePath,
        ModalService modalService
    )
    {
        Activator = new ViewModelActivator();

        FilePath = filePath;

        GitDiffFileInteraction = new Interaction<string, Unit>();

        GitDiffFileCmd = ReactiveCommand.CreateFromTask(() =>
            gitDiffFileHandler.Execute(new GetGitDiffFile.Query(dir, filePath))
        );
        GitDiffFileCmd.ThrownExceptions.Subscribe(x => modalService.ShowErrorDlg(x.ToString()));

        this.WhenActivated(d =>
        {
            GitDiffFileCmd
                .Subscribe(async x => await GitDiffFileInteraction.Handle(x))
                .DisposeWith(d);
            GitDiffFileCmd.Execute().Subscribe().DisposeWith(d);
        });
    }

    private ReactiveCommand<Unit, string> GitDiffFileCmd { get; }
    public IInteraction<string, Unit> GitDiffFileInteraction { get; }
    public string FilePath { get; init; }
    public ViewModelActivator Activator { get; }
}
