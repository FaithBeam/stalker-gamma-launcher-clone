using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using DynamicData;
using ReactiveUI;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs.ModsTab;

public class ModsTabVm : ViewModelBase, IActivatableViewModel
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private readonly ReadOnlyObservableCollection<UpdateableModVm> _updateableMods;

    public ModsTabVm(ModDb modDb, ProgressService progressService)
    {
        Activator = new ViewModelActivator();
        var modDb1 = modDb;
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
                .Select(x => ParseModListRecord.ParseLine(x, modDb))
                .Where(x => x is DownloadableRecord)
                .Cast<DownloadableRecord>()
                .ToList();

            var updatedRecords = (
                await Curl.GetStringAsync("https://stalker-gamma.com/api/list?key=")
            )
                .Split("\n")
                .Select(x => ParseModListRecord.ParseLine(x, modDb1))
                .Where(x => x is DownloadableRecord)
                .Cast<DownloadableRecord>()
                .Where(onlineRec => ShouldUpdateModFilter(localModListRecords, onlineRec))
                .Select(onlineRec => new UpdateableModVm(
                    onlineRec.AddonName!,
                    GetLocalDlRecordFromFilter(localModListRecords, onlineRec)?.Md5ModDb,
                    onlineRec.Md5ModDb!,
                    onlineRec.ModDbUrl!,
                    onlineRec.ZipName!
                ));
            modsSourceCache.Edit(inner =>
            {
                inner.Clear();
                inner.AddOrUpdate(updatedRecords);
            });
        });
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

    public ReactiveCommand<Unit, Unit> GetOnlineModsCmd { get; }

    public ReadOnlyObservableCollection<UpdateableModVm> UpdateableMods => _updateableMods;
    public ViewModelActivator Activator { get; }
}
