using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace stalker_gamma_gui.Controls.Tabs;

public partial class MainTabVm
    : ReactiveUserControl<stalker_gamma.core.ViewModels.Tabs.MainTab.MainTabVm>
{
    private bool _loaded;

    public MainTabVm()
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

            // strobe install / update gamma when user has not updated enough
            this.WhenAnyValue(
                    x => x.ViewModel!.LocalGammaVersion,
                    selector: localGammaVersion => localGammaVersion
                )
                .Where(x => !string.IsNullOrEmpty(x))
                .Subscribe(lgv =>
                {
                    switch (lgv)
                    {
                        case "200":
                        case "865":
                            ToolTip.SetTip(PlayBtn, "You must update / install gamma first");
                            ToolTip.SetShowOnDisabled(PlayBtn, true);
                            InstallUpdateBtn.Classes.Add("Strobing");
                            break;
                        default:
                            ToolTip.SetTip(PlayBtn, null);
                            ToolTip.SetShowOnDisabled(PlayBtn, false);
                            InstallUpdateBtn.Classes.Remove("Strobing");
                            break;
                    }
                });

            // strobe first install initialization
            this.WhenAnyValue(
                    x => x.ViewModel!.IsMo2Initialized,
                    selector: (mo2Initialized) => mo2Initialized
                )
                .Subscribe(x =>
                {
                    if (!x)
                    {
                        ToolTip.SetTip(
                            PlayBtn,
                            "You must initialize Mod Organizer before you can play"
                        );
                        ToolTip.SetShowOnDisabled(PlayBtn, true);
                        FirstInstallInitializeBtn.Classes.Add("Strobing");
                    }
                    else
                    {
                        ToolTip.SetTip(PlayBtn, null);
                        ToolTip.SetShowOnDisabled(PlayBtn, false);
                        FirstInstallInitializeBtn.Classes.Remove("Strobing");
                    }
                });

            // Strobe the downgrade mod organizer button when ran with WINE and mo2 hasn't been downgraded
            this.WhenAnyValue(
                    x => x.ViewModel!.IsMo2VersionDowngraded,
                    x => x.ViewModel!.IsRanWithWine,
                    selector: (mo2Downgraded, ranWithWine) =>
                        (RanWithWine: ranWithWine, Mo2Downgraded: mo2Downgraded)
                )
                .Subscribe(x =>
                {
                    if (x.RanWithWine)
                    {
                        if (x.Mo2Downgraded.HasValue)
                        {
                            if (!x.Mo2Downgraded.Value)
                            {
                                ToolTip.SetTip(
                                    InstallUpdateBtn,
                                    "You must downgrade mod organizer first"
                                );
                                ToolTip.SetShowOnDisabled(InstallUpdateBtn, true);
                                DowngradeModOrganizerBtn.Classes.Add("Strobing");
                            }
                            else
                            {
                                ToolTip.SetTip(InstallUpdateBtn, null);
                                ToolTip.SetShowOnDisabled(InstallUpdateBtn, false);
                                DowngradeModOrganizerBtn.Classes.Remove("Strobing");
                            }
                        }
                    }
                });

            // set strobing for long paths btn
            // set tool tip for install / update gamma
            this.WhenAnyValue(
                    x => x.ViewModel!.LongPathsStatus,
                    x => x.ViewModel!.IsRanWithWine,
                    selector: (longPathsStatus, ranWithWine) =>
                        (
                            LongPathsStatus: longPathsStatus,
                            RanWithWine: ranWithWine,
                            IsWindows: OperatingSystem.IsWindows()
                        )
                )
                .Subscribe(x =>
                {
                    if (x is { IsWindows: true, RanWithWine: false })
                    {
                        if (!x.LongPathsStatus.HasValue || !x.LongPathsStatus.Value)
                        {
                            ToolTip.SetTip(
                                InstallUpdateBtn,
                                "You must enable long paths before you can install / update gamma"
                            );
                            ToolTip.SetShowOnDisabled(InstallUpdateBtn, true);
                            LongPathsBtn.Classes.Add("Strobing");
                        }
                        else
                        {
                            ToolTip.SetTip(InstallUpdateBtn, null);
                            ToolTip.SetShowOnDisabled(InstallUpdateBtn, false);
                            LongPathsBtn.Classes.Remove("Strobing");
                            // LongPathsBtn.Classes.Add("NotStrobing");
                        }
                    }
                })
                .DisposeWith(d);
        });
    }

    private void AppendLineHandler(IInteractionContext<string, Unit> interactionCtx)
    {
        ConsoleOutput.AppendText($"{DateTime.Now:t}:\t{interactionCtx.Input}");
        ConsoleOutput.AppendText(Environment.NewLine);
        ConsoleOutput.ScrollToLine(ConsoleOutput.LineCount);
        interactionCtx.SetOutput(Unit.Default);
    }
}
