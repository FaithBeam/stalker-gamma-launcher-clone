using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Commands;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Models;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Queries;

namespace stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;

public interface IGammaUpdatesVm
{
    ViewModelActivator Activator { get; }
    bool IsLoading { get; }
    IInteraction<GitDiffWindowVm, Unit> OpenGitDiffFileWindowInteraction { get; }
    ReactiveCommand<string, Unit> OpenGitDiffFileWindowCmd { get; }
    ReadOnlyObservableCollection<GitDiff> Diffs { get; }
    IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changing { get; }
    IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changed { get; }
    IObservable<Exception> ThrownExceptions { get; }
    IDisposable SuppressChangeNotifications();
    bool AreChangeNotificationsEnabled();
    IDisposable DelayChangeNotifications();
    event PropertyChangingEventHandler? PropertyChanging;
    event PropertyChangedEventHandler? PropertyChanged;
}

public class GammaUpdatesVm : ViewModelBase, IActivatableViewModel, IGammaUpdatesVm
{
    private readonly ProgressService _ps;
    private readonly GetGitDiffFile.Handler _getGitDiffFileHandler;
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private readonly ReadOnlyObservableCollection<GitDiff> _diffs;
    private readonly ObservableAsPropertyHelper<bool> _isLoading;

    public ViewModelActivator Activator { get; }

    public GammaUpdatesVm(
        ProgressService ps,
        GetGitDiff.Handler getGitDiffHandler,
        GetGitDiffFile.Handler getGitDiffFileHandler,
        GitFetch.Handler gitFetchHandler
    )
    {
        _ps = ps;
        _getGitDiffFileHandler = getGitDiffFileHandler;
        Activator = new ViewModelActivator();

        var gitDiffSourceCache = new SourceCache<GitDiff, string>(x => x.Path);

        gitDiffSourceCache.Connect().Bind(out _diffs).Subscribe();

        OpenGitDiffFileWindowInteraction = new Interaction<GitDiffWindowVm, Unit>();
        OpenGitDiffFileWindowCmd = ReactiveCommand.CreateFromTask<string>(OpenGitDiffWindow);

        var getGitDiffCmd = ReactiveCommand.CreateFromTask(() =>
            Task.Run(async () =>
            {
                await gitFetchHandler.ExecuteAsync(
                    new GitFetch.Command(Path.Join(_dir, "resources", "Stalker_GAMMA"))
                );
                return await getGitDiffHandler.ExecuteAsync(
                    new GetGitDiff.Query(Path.Join(_dir, "resources", "Stalker_GAMMA"))
                );
            })
        );
        _isLoading = getGitDiffCmd.IsExecuting.ToProperty(this, x => x.IsLoading);
        getGitDiffCmd.Subscribe(x =>
            gitDiffSourceCache.Edit(inner =>
            {
                inner.Clear();
                inner.AddOrUpdate(x);
            })
        );
        getGitDiffCmd.ThrownExceptions.Subscribe(x => ps.UpdateProgress(x.ToString()));

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                getGitDiffCmd.Execute().Subscribe();
            }
        );
    }

    private async Task OpenGitDiffWindow(string path)
    {
        await OpenGitDiffFileWindowInteraction.Handle(
            new GitDiffWindowVm(
                _ps,
                _getGitDiffFileHandler,
                Path.Join(_dir, "resources", "Stalker_GAMMA"),
                path
            )
        );
    }

    public bool IsLoading => _isLoading.Value;
    public IInteraction<GitDiffWindowVm, Unit> OpenGitDiffFileWindowInteraction { get; }

    public ReactiveCommand<string, Unit> OpenGitDiffFileWindowCmd { get; }

    public ReadOnlyObservableCollection<GitDiff> Diffs => _diffs;
}
