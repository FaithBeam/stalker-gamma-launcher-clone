using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using DynamicData;
using ReactiveUI;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs.ModDbUpdatesTab;

public interface IModDbUpdatesTabVm
{
    bool IsLoading { get; }
    ReactiveCommand<Unit, Unit> GetOnlineModsCmd { get; }
    ReadOnlyObservableCollection<UpdateableModVm> UpdateableMods { get; }
    ViewModelActivator Activator { get; }
    IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changing { get; }
    IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changed { get; }
    IObservable<Exception> ThrownExceptions { get; }
    IDisposable SuppressChangeNotifications();
    bool AreChangeNotificationsEnabled();
    IDisposable DelayChangeNotifications();
    event PropertyChangingEventHandler? PropertyChanging;
    event PropertyChangedEventHandler? PropertyChanged;
}

public partial class ModDbUpdatesTabVm : ViewModelBase, IActivatableViewModel, IModDbUpdatesTabVm
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private readonly ReadOnlyObservableCollection<UpdateableModVm> _updateableMods;
    private readonly ObservableAsPropertyHelper<bool> _isLoading;

    public ModDbUpdatesTabVm(
        ICurlService curlService,
        ModListRecordFactory modListRecordFactory,
        ProgressService progressService
    )
    {
        Activator = new ViewModelActivator();
        var modListFile = Path.Join(_dir, "mods.txt");

        SourceCache<UpdateableModVm, string> modsSourceCache = new(x => x.AddonName);
        var obs = modsSourceCache.Connect().Bind(out _updateableMods).Subscribe();

        GetOnlineModsCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            if (!File.Exists(modListFile))
            {
                progressService.UpdateProgress($"Mods list file not found: {modListFile}");
                return;
            }

            var localModListRecords = File.ReadAllLines(modListFile)
                .Select(x => modListRecordFactory.Create(x))
                .Where(x => x is DownloadableRecord)
                .Cast<DownloadableRecord>()
                .ToList();

            var updatedRecords = (
                await curlService.GetStringAsync("https://stalker-gamma.com/api/list?key=")
            )
                .Split("\n")
                .Select(x => modListRecordFactory.Create(x))
                .Where(x => x is DownloadableRecord)
                .Cast<DownloadableRecord>()
                .Where(onlineRec => ShouldUpdateModFilter(localModListRecords, onlineRec))
                .Select(onlineRec =>
                {
                    var localDlRec = GetLocalDlRecordFromFilter(localModListRecords, onlineRec);
                    var localVersion = FileNameVersionRx()
                        .Match(Path.GetFileNameWithoutExtension(localDlRec?.ZipName ?? ""))
                        .Groups[1]
                        .Value;
                    localVersion = string.IsNullOrWhiteSpace(localVersion)
                        ? Path.GetFileNameWithoutExtension(localDlRec?.ZipName ?? "")
                        : localVersion;
                    var remoteVersion = FileNameVersionRx()
                        .Match(Path.GetFileNameWithoutExtension(onlineRec.ZipName ?? ""))
                        .Groups[1]
                        .Value;
                    remoteVersion = string.IsNullOrWhiteSpace(remoteVersion)
                        ? Path.GetFileNameWithoutExtension(onlineRec.ZipName ?? "")
                        : remoteVersion;
                    var updateType =
                        IsAddMod(localModListRecords, onlineRec) ? UpdateType.Add
                        : IsUpdateMod(localModListRecords, onlineRec) ? UpdateType.Update
                        : UpdateType.None;
                    return new UpdateableModVm(
                        onlineRec.AddonName!,
                        onlineRec.ModDbUrl!,
                        localVersion,
                        remoteVersion,
                        updateType
                    );
                });
            modsSourceCache.Edit(inner =>
            {
                inner.Clear();
                inner.AddOrUpdate(updatedRecords);
            });
        });
        _isLoading = GetOnlineModsCmd.IsExecuting.ToProperty(this, x => x.IsLoading);
        GetOnlineModsCmd.ThrownExceptions.Subscribe(ex =>
            progressService.UpdateProgress(ex.ToString())
        );

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                GetOnlineModsCmd.Execute().Subscribe();
            }
        );
    }

    private static readonly Func<List<DownloadableRecord>, DownloadableRecord, bool> IsAddMod = (
        localModListRecs,
        onlineRec
    ) => localModListRecs.All(localRec => localRec.ModDbUrl! != onlineRec.ModDbUrl!);

    private static readonly Func<List<DownloadableRecord>, DownloadableRecord, bool> IsUpdateMod = (
        localModListRecords,
        onlineRec
    ) =>
        localModListRecords.Any(localRec =>
            localRec.ModDbUrl! == onlineRec.ModDbUrl! && localRec.Md5ModDb != onlineRec.Md5ModDb
        );
    private static readonly Func<
        List<DownloadableRecord>,
        DownloadableRecord,
        DownloadableRecord?
    > GetLocalDlRecordFromFilter = (localModListRecs, onlineRec) =>
        localModListRecs.FirstOrDefault(localRec =>
            localRec.ModDbUrl! == onlineRec.ModDbUrl! && localRec.Md5ModDb != onlineRec.Md5ModDb
        );

    private static readonly Func<
        List<DownloadableRecord>,
        DownloadableRecord,
        bool
    > ShouldUpdateModFilter = (localModListRecords, onlineRec) =>
        localModListRecords.Any(localRec =>
            localRec.ModDbUrl! == onlineRec.ModDbUrl! && localRec.Md5ModDb != onlineRec.Md5ModDb
        ) || localModListRecords.All(localRec => localRec.ModDbUrl! != onlineRec.ModDbUrl!);

    public bool IsLoading => _isLoading.Value;
    public ReactiveCommand<Unit, Unit> GetOnlineModsCmd { get; }

    public ReadOnlyObservableCollection<UpdateableModVm> UpdateableMods => _updateableMods;
    public ViewModelActivator Activator { get; }

    [GeneratedRegex(@"^.+(\d+\.\d+\.\d*.*)$")]
    private static partial Regex FileNameVersionRx();
}
