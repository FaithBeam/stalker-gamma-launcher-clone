using System.Text.RegularExpressions;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.ViewModels.Tabs.ModDbUpdatesTab;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;

public static partial class DiffMods
{
    public sealed record Query(LocalAndRemoteVersion ModVersions);

    public sealed partial class Handler(
        ICurlService curlService,
        ModListRecordFactory modListRecordFactory
    )
    {
        private readonly ICurlService _curlService = curlService;
        private readonly ModListRecordFactory _modListRecordFactory = modListRecordFactory;

        public async Task<List<string>> Execute(Query q)
        {
            var localModListRecords =
                q.ModVersions.LocalVersion?.Split(Environment.NewLine)
                    .Select(x => _modListRecordFactory.Create(x))
                    .Where(x => x is DownloadableRecord)
                    .Cast<DownloadableRecord>()
                    .ToList() ?? [];

            var updatedRecords = (
                await _curlService.GetStringAsync("https://stalker-gamma.com/api/list?key=")
            )
                .Split("\n")
                .Select(x => _modListRecordFactory.Create(x))
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

            return updatedRecords
                .Select(x =>
                    $"{x.UpdateType switch {
                        UpdateType.Add => "+", UpdateType.Remove => "-", UpdateType.Update => "^", UpdateType.None => "?", _ => throw new ArgumentOutOfRangeException() }} {x.AddonName}"
                )
                .ToList();
        }

        private static readonly Func<
            List<DownloadableRecord>,
            DownloadableRecord,
            DownloadableRecord?
        > GetLocalDlRecordFromFilter = (localModListRecs, onlineRec) =>
            localModListRecs.FirstOrDefault(localRec =>
                localRec.ModDbUrl! == onlineRec.ModDbUrl! && localRec.Md5ModDb != onlineRec.Md5ModDb
            );

        private static readonly Func<List<DownloadableRecord>, DownloadableRecord, bool> IsAddMod =
            (localModListRecs, onlineRec) =>
                localModListRecs.All(localRec => localRec.ModDbUrl! != onlineRec.ModDbUrl!);

        private static readonly Func<
            List<DownloadableRecord>,
            DownloadableRecord,
            bool
        > IsUpdateMod = (localModListRecords, onlineRec) =>
            localModListRecords.Any(localRec =>
                localRec.ModDbUrl! == onlineRec.ModDbUrl! && localRec.Md5ModDb != onlineRec.Md5ModDb
            );

        private static readonly Func<
            List<DownloadableRecord>,
            DownloadableRecord,
            bool
        > ShouldUpdateModFilter = (localModListRecords, onlineRec) =>
            IsUpdateMod(localModListRecords, onlineRec) || IsAddMod(localModListRecords, onlineRec);

        [GeneratedRegex(@"^.+(\d+\.\d+\.\d*.*)$")]
        private static partial Regex FileNameVersionRx();
    }
}
