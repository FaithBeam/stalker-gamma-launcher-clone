using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
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

                ViewModel.AppendLineInteraction.RegisterHandler(AppendLineHandler);
                ViewModel.ChangeGammaBackupDirectoryInteraction.RegisterHandler(
                    ChangeGammaBackupDirectory
                );
            }
        );
    }

    private async Task ChangeGammaBackupDirectory(IInteractionContext<Unit, string?> ctx)
    {
        var fileNames = await TopLevel
            .GetTopLevel(this)!
            .StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "Select your GAMMA backup folder",
                    AllowMultiple = false,
                }
            );
        ctx.SetOutput(fileNames.Any() ? fileNames[0].TryGetLocalPath() : null);
    }

    private void AppendLineHandler(IInteractionContext<string, Unit> interactionCtx)
    {
        BackupTxt.AppendText($"{DateTime.Now:t}:\t{interactionCtx.Input}");
        BackupTxt.AppendText(Environment.NewLine);
        BackupTxt.ScrollToLine(BackupTxt.LineCount);
        interactionCtx.SetOutput(Unit.Default);
    }
}
