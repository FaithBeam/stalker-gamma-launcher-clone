using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using DynamicData;
using ReactiveUI;
using stalker_gamma.core.Factories;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Utilities;

namespace stalker_gamma_gui.ViewModels.Tabs.ModDbUpdatesTab;

public partial class ModDbUpdatesTabVm : ViewModelBase, IActivatableViewModel
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private readonly ReadOnlyObservableCollection<UpdateableModVm> _updateableMods;
    private readonly ObservableAsPropertyHelper<bool> _isLoading;

    public ModDbUpdatesTabVm(ModListRecordFactory modListRecordFactory)
    {
        Activator = new ViewModelActivator();
        var modListFile = Path.Join(_dir, "mods.txt");

        SourceCache<UpdateableModVm, string> modsSourceCache = new(x => x.AddonName);
        var obs = modsSourceCache.Connect().Bind(out _updateableMods).Subscribe();

        GetOnlineModsCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            if (!File.Exists(modListFile))
            {
                return;
            }

            var localModListRecords = File.ReadAllLines(modListFile)
                .Select(modListRecordFactory.Create)
                .Where(x => x is DownloadableRecord)
                .Cast<DownloadableRecord>()
                .ToList();

            var updatedRecords = (
                await CurlUtility.GetStringAsync("https://stalker-gamma.com/api/list?key=")
            )
                .Split("\n")
                .Select(modListRecordFactory.Create)
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
