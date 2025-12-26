using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace stalker_gamma_gui.Controls.Tabs;

public partial class MainTabVm
    : ReactiveUserControl<stalker_gamma_gui.ViewModels.Tabs.MainTab.MainTabVm>
{
    public MainTabVm()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            if (ViewModel is null)
            {
                return;
            }

            // strobe install / update gamma when user has not updated enough
            const string strobing = "Strobing";
            this.WhenAnyValue(
                    x => x.ViewModel!.LocalGammaVersion,
                    selector: localGammaVersion => int.Parse(localGammaVersion ?? "0")
                )
                .Subscribe(lgv =>
                {
                    if (lgv < 920)
                    {
                        ToolTip.SetTip(PlayBtn, "You must update / install gamma first");
                        ToolTip.SetShowOnDisabled(PlayBtn, true);
                        InstallUpdateBtn.Classes.Add(strobing);
                    }
                    else
                    {
                        ToolTip.SetTip(PlayBtn, null);
                        ToolTip.SetShowOnDisabled(PlayBtn, false);
                        InstallUpdateBtn.Classes.Remove(strobing);
                    }
                })
                .DisposeWith(d);

            // strobe first install initialization
            this.WhenAnyValue(
                    x => x.ViewModel!.IsMo2Initialized,
                    selector: mo2Initialized => mo2Initialized
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
                        FirstInstallInitializeBtn.Classes.Add(strobing);
                    }
                    else
                    {
                        ToolTip.SetTip(PlayBtn, null);
                        ToolTip.SetShowOnDisabled(PlayBtn, false);
                        FirstInstallInitializeBtn.Classes.Remove(strobing);
                    }
                })
                .DisposeWith(d);

            // set strobing for long paths btn
            // set tool tip for install / update gamma
            this.WhenAnyValue(
                    x => x.ViewModel!.LongPathsStatus,
                    x => x.ViewModel!.IsRanWithWine,
                    selector: (longPathsStatus, ranWithWine) =>
                        (
                            LongPathsStatus: longPathsStatus,
                            RanWithWine: ranWithWine,
                            IsWindows: OperatingSystem.IsWindows(),
                            IsLinux: OperatingSystem.IsLinux(),
                            IsMacOS: OperatingSystem.IsMacOS()
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
                            LongPathsBtn.Classes.Add(strobing);
                        }
                        else
                        {
                            ToolTip.SetTip(InstallUpdateBtn, null);
                            ToolTip.SetShowOnDisabled(InstallUpdateBtn, false);
                            LongPathsBtn.Classes.Remove(strobing);
                        }
                    }
                })
                .DisposeWith(d);
        });
    }
}
