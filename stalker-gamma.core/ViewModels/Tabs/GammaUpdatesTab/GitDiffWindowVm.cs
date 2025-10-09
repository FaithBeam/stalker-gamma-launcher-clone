using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Queries;

namespace stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;

public class GitDiffWindowVm : ViewModelBase, IActivatableViewModel
{
    public GitDiffWindowVm(
        ProgressService ps,
        GetGitDiffFile.Handler gitDiffFileHandler,
        string dir,
        string filePath
    )
    {
        Activator = new ViewModelActivator();

        FilePath = filePath;

        GitDiffFileInteraction = new Interaction<string, Unit>();

        GitDiffFileCmd = ReactiveCommand.CreateFromTask(async () =>
            await gitDiffFileHandler.ExecuteAsync(new GetGitDiffFile.Query(dir, filePath))
        );
        GitDiffFileCmd.Subscribe(async x => await GitDiffFileInteraction.Handle(x));
        GitDiffFileCmd.ThrownExceptions.Subscribe(x => ps.UpdateProgress(x.ToString()));

        this.WhenActivated(d =>
        {
            GitDiffFileCmd.Execute().Subscribe().DisposeWith(d);
        });
    }

    private ReactiveCommand<Unit, string> GitDiffFileCmd { get; }
    public IInteraction<string, Unit> GitDiffFileInteraction { get; }
    public string FilePath { get; init; }
    public ViewModelActivator Activator { get; }
}
