using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using CliWrap;
using DynamicData;
using ReactiveUI;
using stalker_gamma.core.Services;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;

public class GammaUpdatesTabVm : ViewModelBase, IActivatableViewModel
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private readonly ReadOnlyObservableCollection<GammaUpdateRecord> _updateableRecords;
    private readonly SourceCache<GammaUpdateRecord, string> _gammaModUpdatesSourceCache;

    public GammaUpdatesTabVm(ProgressService progressService)
    {
        Activator = new ViewModelActivator();

        _gammaModUpdatesSourceCache = new SourceCache<GammaUpdateRecord, string>(x => x.Path);
        var obs = _gammaModUpdatesSourceCache.Connect().Bind(out _updateableRecords).Subscribe();

        GetUpdatesCmd = ReactiveCommand.CreateFromTask(GetUpdatesAsync);
        GetUpdatesCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""
                {x.Message}
                {x.StackTrace}
                """
            )
        );

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                GetUpdatesCmd.Execute().Subscribe();
            }
        );
    }

    private async Task GetUpdatesAsync()
    {
        var stdErr = new StringBuilder();
        var stdOut = new StringBuilder();
        var gitCmdPath = OperatingSystem.IsWindows()
            ? Path.GetFullPath(Path.Join("resources", "bin", "git.exe"))
            : "git";
        await Cli.Wrap(gitCmdPath)
            .WithArguments(["diff-tree", "-r", "--name-status", "origin/main"])
            .WithWorkingDirectory(Path.Join(_dir, "resources", "Stalker_GAMMA"))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
            .ExecuteAsync();
        _gammaModUpdatesSourceCache.Clear();
        _gammaModUpdatesSourceCache.Edit(inner =>
            inner.AddOrUpdate(
                stdOut
                    .ToString()
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split('\t'))
                    .Where(x => x.Length == 2)
                    .Select(parts => new GammaUpdateRecord(
                        parts[1],
                        parts[0] switch
                        {
                            "A" => GammaUpdateType.Added,
                            "M" => GammaUpdateType.Modified,
                            "D" => GammaUpdateType.Removed,
                            _ => throw new ArgumentOutOfRangeException(parts[0]),
                        }
                    ))
            )
        );
    }

    public ReadOnlyObservableCollection<GammaUpdateRecord> UpdateableRecords => _updateableRecords;
    private ReactiveCommand<Unit, Unit> GetUpdatesCmd { get; }

    public ViewModelActivator Activator { get; }
}
