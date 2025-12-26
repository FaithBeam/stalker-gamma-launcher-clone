using System;
using System.Net;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using stalker_gamma_gui.Services;
using stalker_gamma_gui.ViewModels.Dialogs.DownloadProgress;
using stalker_gamma_gui.ViewModels.MainWindow;
using stalker_gamma_gui.ViewModels.Services;
using stalker_gamma_gui.ViewModels.Tabs;
using stalker_gamma_gui.ViewModels.Tabs.BackupTab;
using stalker_gamma_gui.ViewModels.Tabs.GammaUpdatesTab;
using stalker_gamma_gui.ViewModels.Tabs.MainTab;
using stalker_gamma_gui.ViewModels.Tabs.ModDbUpdatesTab;
using stalker_gamma_gui.ViewModels.Tabs.ModListTab;
using stalker_gamma_gui.Views;
using stalker_gamma.core.Factories;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstallerServices;
using stalker_gamma.core.Services.ModOrganizer;
using stalker_gamma.core.Services.ModOrganizer.DownloadModOrganizer;
using stalker_gamma.core.Services.Shortcut;
using stalker_gamma.core.Utilities;
using GammaInstaller = stalker_gamma_gui.Services.GammaInstaller.GammaInstaller;
using ProgressService = stalker_gamma_gui.Services.ProgressService;

namespace stalker_gamma_gui;

public partial class App : Application
{
    private IServiceProvider? _container;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        var mainWindow = new MainWindow();

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(s =>
            {
                s.UseMicrosoftDependencyResolver();

                s.AddHttpClient(
                    Options.DefaultName,
                    client =>
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "stalker-gamma-clone/1.0");
                    }
                );
                s.AddHttpClient(
                        "githubDlArchive",
                        client =>
                        {
                            client.DefaultRequestHeaders.Add(
                                "User-Agent",
                                "stalker-gamma-clone/1.0"
                            );
                        }
                    )
                    .ConfigurePrimaryHttpMessageHandler(() =>
                        new SocketsHttpHandler
                        {
                            EnableMultipleHttp2Connections = true,
                            AutomaticDecompression = DecompressionMethods.None,
                        }
                    );

                s.AddSingleton(
                    new GlobalSettings
                    {
#pragma warning disable IL2026
                        DownloadThreads = configuration.GetValue<int>("downloadThreads"),
                        ExtractThreads = configuration.GetValue<int>("extractThreads"),
                        GammaBackupPath = configuration.GetValue<string>("gammaBackupPath"),
                        CheckForLauncherUpdates = configuration.GetValue<bool>(
                            "checkForLauncherUpdates"
                        ),
                        ForceBorderlessFullscreen = configuration.GetValue<bool>(
                            "forceBorderlessFullscreen"
                        ),
                        StalkerAnomalyArchiveMd5 =
                            configuration.GetValue<string>("anomalyArchiveMd5") ?? ""
#pragma warning restore IL2026
                    }
                );

                s.AddSingleton(new FilePickerService(mainWindow));

                s.AddSingleton<SettingsFileService>();

                s.AddSingleton<ProgressService>()
                    .AddSingleton<IVersionService, VersionService>()
                    .AddSingleton<IIsBusyService, IsBusyService>()
                    .AddSingleton<ModalService>();

                s.AddScoped<DownloadModOrganizerService>()
                    .AddScoped<AnomalyInstaller>()
                    .AddScoped<WriteModOrganizerIniService>()
                    .AddScoped<IIsRanWithWineService, IsRanWithWineService>()
                    .AddScoped<IILongPathsStatusService, LongPathsStatus.Handler>()
                    .AddScoped<ICurlService, CurlService>()
                    .AddScoped<MirrorService>()
                    .AddScoped<ModDb>()
                    .AddScoped<ModListRecordFactory>()
                    .AddScoped<Shortcut>()
                    .AddScoped<GammaInstaller>();

                s.RegisterCommonTabServices()
                    .RegisterDownloadProgressServices()
                    .RegisterMainWindowServices()
                    .RegisterBackupTabServices()
                    .RegisterMainTabServices()
                    .RegisterGammaUpdatesTabServices();

                s.AddScoped<MainTabVm>()
                    .AddScoped<BackupTabVm>()
                    .AddScoped<GammaUpdatesVm>()
                    .AddScoped<ModListTabVm>()
                    .AddScoped<ModDbUpdatesTabVm>()
                    .AddScoped<MainWindowVm>();

                var resolver = Locator.CurrentMutable;
                resolver.InitializeSplat();
                resolver.InitializeReactiveUI();

                s.AddSingleton<IActivationForViewFetcher, AvaloniaActivationForViewFetcher>();
            })
            .Build();

        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mainWindow;

            _container = host.Services;
            _container.UseMicrosoftDependencyResolver();

            desktop.MainWindow.DataContext = _container.GetRequiredService<MainWindowVm>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
