using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using stalker_gamma_gui.Views;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.DowngradeModOrganizer;
using stalker_gamma.core.Services.GammaInstaller;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.Anomaly;
using stalker_gamma.core.Services.GammaInstaller.Mo2;
using stalker_gamma.core.Services.GammaInstaller.ModpackSpecific;
using stalker_gamma.core.Services.GammaInstaller.Shortcut;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;
using stalker_gamma.core.ViewModels.MainWindow;
using stalker_gamma.core.ViewModels.Tabs;
using stalker_gamma.core.ViewModels.Tabs.BackupTab;
using stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;
using stalker_gamma.core.ViewModels.Tabs.MainTab;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Commands;
using stalker_gamma.core.ViewModels.Tabs.ModDbUpdatesTab;
using stalker_gamma.core.ViewModels.Tabs.ModListTab;

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
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(s =>
            {
                s.UseMicrosoftDependencyResolver();

                s.AddHttpClient();

                s.AddSingleton(
                    new GlobalSettings
                    {
#pragma warning disable IL2026
                        UseCurlImpersonate = configuration.GetValue<bool>("useCurlImpersonate"),
                        GammaBackupPath = configuration.GetValue<string>("gammaBackupPath"),
#pragma warning restore IL2026
                    }
                );

                s.AddSingleton<ProgressService>()
                    .AddSingleton<VersionService>()
                    .AddSingleton<IIsBusyService, IsBusyService>()
                    .AddSingleton<IOperatingSystemService, OperatingSystemService>();

                s.AddScoped<DowngradeModOrganizer>()
                    .AddScoped<IIsRanWithWineService, IsRanWithWineService>()
                    .AddScoped<IILongPathsStatusService, LongPathsStatus.Handler>()
                    .AddScoped<ICurlService, CurlService>()
                    .AddScoped<MirrorService>()
                    .AddScoped<GitUtility>()
                    .AddScoped<ModDb>()
                    .AddScoped<ModListRecordFactory>()
                    .AddScoped<AddonsAndSeparators>()
                    .AddScoped<Anomaly>()
                    .AddScoped<Mo2>()
                    .AddScoped<ModpackSpecific>()
                    .AddScoped<Shortcut>()
                    .AddScoped<GammaInstaller>();

                s.RegisterCommonTabServices()
                    .RegisterBackupTabServices()
                    .RegisterMainTabServices()
                    .RegisterGammaUpdatesTabServices();

                s.AddScoped<IMainTabVm, MainTabVm>()
                    .AddScoped<IBackupTabVm, BackupTabVm>()
                    .AddScoped<IGammaUpdatesVm, GammaUpdatesVm>()
                    .AddScoped<IModListTabVm, ModListTabVm>()
                    .AddScoped<IModDbUpdatesTabVm, ModDbUpdatesTabVm>()
                    .AddScoped<IMainWindowVm, MainWindowVm>();

                var resolver = Locator.CurrentMutable;
                resolver.InitializeSplat();
                resolver.InitializeReactiveUI();

                s.AddSingleton<IActivationForViewFetcher, AvaloniaActivationForViewFetcher>();
            })
            .Build();

        _container = host.Services;
        _container.UseMicrosoftDependencyResolver();

        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _container.GetRequiredService<IMainWindowVm>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
