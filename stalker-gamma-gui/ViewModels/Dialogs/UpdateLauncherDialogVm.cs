using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using ReactiveUI;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;

namespace stalker_gamma_gui.ViewModels.Dialogs;

public class UpdateLauncherDialogVm : ReactiveObject, IActivatableViewModel
{
    private ObservableAsPropertyHelper<DoUpdateCmdParam?>? _doUpdate;

    public ViewModelActivator Activator { get; }
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public UpdateLauncherDialogVm(UpdateAvailable.Response info, GlobalSettings globalSettings)
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
                    var downloadProc = Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "stalker-gamma.updater.exe",
                            ArgumentList = { "download" },
                            UseShellExecute = true,
                        }
                    );
                    downloadProc?.WaitForExit();
                    if (downloadProc?.ExitCode == 1)
                    {
                        throw new Exception("Update failed");
                    }
                    var stalkerGammaUpdaterTempDir = Path.Join(
                        Path.GetTempPath(),
                        "stalker-gamma-updater"
                    );
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = Path.Join(
                                stalkerGammaUpdaterTempDir,
                                "stalker-gamma.updater.exe"
                            ),
                            WorkingDirectory = stalkerGammaUpdaterTempDir,
                            ArgumentList = { "copy", "--destination", _dir },
                            UseShellExecute = true,
                            CreateNoWindow = true,
                        }
                    );
                },
                ct
            )
        );
        DoUpdateCmd = ReactiveCommand.Create<DoUpdateCmdParam, DoUpdateCmdParam?>(x => x);
        DoNotAskAgainCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            globalSettings.CheckForLauncherUpdates = false;
            await globalSettings.WriteAppSettingsAsync();
        });

        DoUpdateCmd.Subscribe(doUpdate =>
        {
            switch (doUpdate)
            {
                case DoUpdateCmdParam.Yes:
                    ActuallyDoUpdateCmd.Execute().Subscribe();
                    break;
                case DoUpdateCmdParam.NoAndDisable:
                    DoNotAskAgainCmd.Execute().Subscribe();
                    break;
                case DoUpdateCmdParam.No:
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(doUpdate), doUpdate, null);
            }
        });

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                _doUpdate = DoUpdateCmd.ToProperty(this, x => x.DoUpdate).DisposeWith(d);
            }
        );
    }

    public string CurrentVersion { get; }
    public string RemoteVersion { get; }
    public string ChangeNotes { get; }
    public string Link { get; }

    public DoUpdateCmdParam? DoUpdate => _doUpdate?.Value;

    public ReactiveCommand<DoUpdateCmdParam, DoUpdateCmdParam?> DoUpdateCmd { get; }
    private ReactiveCommand<Unit, Unit> ActuallyDoUpdateCmd { get; }
    private ReactiveCommand<Unit, Unit> DoNotAskAgainCmd { get; }
}

public enum DoUpdateCmdParam
{
    Yes,
    No,
    NoAndDisable,
}
