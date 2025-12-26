using System.Reactive.Disposables;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using AvaloniaEdit.TextMate;
using ReactiveUI;
using stalker_gamma_gui.ViewModels.Dialogs;
using TextMateSharp.Grammars;

namespace stalker_gamma_gui.Controls.Dialogs;

public partial class UpdateDialog : ReactiveWindow<UpdateLauncherDialogVm>
{
    public UpdateDialog()
    {
        InitializeComponent();

        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var textMateInstallation = TextEditor.InstallTextMate(registryOptions);
        textMateInstallation.SetGrammar(
            registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".md").Id)
        );

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                if (ViewModel is null)
                {
                    return;
                }

                TextEditor.AppendText(
                    $"""
                    An update is available! Would you like to update?

                    Your version: {ViewModel.CurrentVersion}
                    Remote version: {ViewModel.RemoteVersion}

                    Link: {ViewModel.Link}

                    Change Notes:
                    {ViewModel.ChangeNotes}
                    """
                );
            }
        );
    }

    private void YesBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void NoBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void NoDoNotAskAgainBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
