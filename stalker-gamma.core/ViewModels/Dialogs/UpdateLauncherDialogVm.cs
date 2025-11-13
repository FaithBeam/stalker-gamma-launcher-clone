using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;
using stalker_gamma.core.ViewModels.MainWindow.Queries;

namespace stalker_gamma.core.ViewModels.Dialogs;

public class UpdateLauncherDialogVm : ReactiveObject, IActivatableViewModel
{
    private readonly ObservableAsPropertyHelper<DoUpdateCmdParam?> _doUpdate;

    public ViewModelActivator Activator { get; }
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public UpdateLauncherDialogVm(UpdateAvailable.Response info)
    {
        Activator = new ViewModelActivator();

        CurrentVersion = info.CurrentVersion;
        RemoteVersion = info.LatestVersion;
        ChangeNotes = info.ChangeNotes;
        Link = info.Link;

        ActuallyDoUpdateCmd = ReactiveCommand.CreateFromTask(async ct =>
            await Task.Run(
                () =>
                {
                    var extractDir = _dir;
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "stalker-gamma.updater.exe",
                            ArgumentList =
                            {
                                $"{Environment.ProcessId}",
                                info.DownloadLink,
                                extractDir,
                            },
                            UseShellExecute = true,
                        }
                    );
                },
                ct
            )
        );
        DoUpdateCmd = ReactiveCommand.Create<DoUpdateCmdParam, DoUpdateCmdParam?>(x => x);
        DoUpdateCmd.Subscribe(doUpdate =>
        {
            if (doUpdate == DoUpdateCmdParam.Yes)
            {
                ActuallyDoUpdateCmd.Execute().Subscribe();
            }
        });
        _doUpdate = DoUpdateCmd.ToProperty(this, x => x.DoUpdate);

        this.WhenActivated((CompositeDisposable d) => { });
    }

    public string CurrentVersion { get; }
    public string RemoteVersion { get; }
    public string ChangeNotes { get; }
    public string Link { get; }

    public DoUpdateCmdParam? DoUpdate => _doUpdate.Value;

    public ReactiveCommand<DoUpdateCmdParam, DoUpdateCmdParam?> DoUpdateCmd { get; }
    private ReactiveCommand<Unit, Unit> ActuallyDoUpdateCmd { get; }
}

public enum DoUpdateCmdParam
{
    Yes,
    No,
    NoAndDisable,
}
