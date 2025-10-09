using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using AvaloniaEdit.TextMate;
using ReactiveUI;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;
using TextMateSharp.Grammars;

namespace stalker_gamma_gui.Controls.Tabs;

public partial class DiffWindow : ReactiveWindow<GitDiffWindowVm>
{
    public DiffWindow()
    {
        InitializeComponent();

        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var textMateInstallation = DiffEdit.InstallTextMate(registryOptions);
        textMateInstallation.SetGrammar(
            registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".diff").Id)
        );

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                if (ViewModel is null)
                {
                    return;
                }

                ViewModel.GitDiffFileInteraction.RegisterHandler(HandleGitDiffFile);
            }
        );
    }

    private void HandleGitDiffFile(IInteractionContext<string, Unit> ctx)
    {
        DiffEdit.AppendText(ctx.Input);
        ctx.SetOutput(Unit.Default);
    }
}
