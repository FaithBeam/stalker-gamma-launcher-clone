using System;
using System.IO;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.Tabs;
using stalker_gamma.core.ViewModels.Tabs.MainTab;

namespace stalker_gamma_gui.Controls.Tabs;

public partial class MainTab : ReactiveUserControl<MainTabVm>
{
    private bool _loaded;

    public MainTab()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            if (ViewModel is null)
            {
                return;
            }

            if (!_loaded)
            {
                ConsoleOutput.AppendText(
                    $"""
                      
                      
                      Welcome to the Gigantic Automated Modular Modpack for Anomaly installer
                      
                      Be sure to check out the discord #how-to-install channel for full instructions:   https://www.discord.gg/stalker-gamma
                      
                      Untick Check MD5 ONLY if your pack is already working and you want to update it.
                      
                      Check the update status above and click Install/Update GAMMA if needed.
                      
                      Currently working from the {Path.GetDirectoryName(
                          AppContext.BaseDirectory
                      )} directory.

                     """
                );
            }

            _loaded = true;

            ViewModel.AppendLineInteraction.RegisterHandler(AppendLineHandler);
        });
    }

    private void AppendLineHandler(IInteractionContext<string, Unit> interactionCtx)
    {
        ConsoleOutput.AppendText(interactionCtx.Input);
        ConsoleOutput.AppendText(Environment.NewLine);
        ConsoleOutput.ScrollToLine(ConsoleOutput.LineCount);
        interactionCtx.SetOutput(Unit.Default);
    }
}
