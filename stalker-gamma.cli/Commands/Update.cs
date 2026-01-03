using System.Text.Json;
using ConsoleAppFramework;
using Serilog;
using stalker_gamma.cli.Models;
using stalker_gamma.cli.Utilities;
using Stalker.Gamma.Extensions;
using Stalker.Gamma.Factories;
using Stalker.Gamma.GammaInstallerServices;
using Stalker.Gamma.Models;

namespace stalker_gamma.cli.Commands;

[RegisterCommands]
public class UpdateCmds(
    ILogger logger,
    CliSettings cliSettings,
    StalkerGammaSettings stalkerGammaSettings,
    IGetStalkerModsFromApi getStalkerModsFromApi,
    IModListRecordFactory modListRecordFactory
)
{
    [Command("update")]
    public async Task ListUpdates()
    {
        ValidateActiveProfile.Validate(_logger, _cliSettings.ActiveProfile);
        stalkerGammaSettings.ModpackMakerList = _cliSettings.ActiveProfile!.ModPackMakerUrl;

        var localModPackMakerPath = Path.Join(
            _cliSettings.ActiveProfile!.Gamma,
            "profiles",
            _cliSettings.ActiveProfile.Mo2Profile,
            "modpack_maker_list.json"
        );

        var localRecords = File.Exists(localModPackMakerPath)
            ? JsonSerializer.Deserialize(
                await File.ReadAllTextAsync(localModPackMakerPath),
                jsonTypeInfo: ModPackMakerCtx.Default.ListModPackMakerRecord
            ) ?? []
            : [];
        var onlineRecordsTxt = await _getStalkerModsFromApi.GetModsAsync();
        var onlineRecords = _modListRecordFactory.Create(onlineRecordsTxt);
        var diffs = localRecords.Diff(onlineRecords);
        if (diffs.Count > 0)
        {
            var olds = diffs
                .Where(x =>
                    x.OldListRecord is not null
                    && !string.IsNullOrWhiteSpace(x.OldListRecord.AddonName)
                )
                .Select(x => x.OldListRecord!);
            var news = diffs
                .Where(x =>
                    x.NewListRecord is not null
                    && !string.IsNullOrWhiteSpace(x.NewListRecord.AddonName)
                )
                .Select(x => x.NewListRecord!);
            var joined = olds.Concat(news).ToList();
            var padRightAddonName = joined.MaxBy(x => x.AddonName!.Length)!.AddonName!.Length + 5;
            var padRightOldZipName =
                diffs.MaxBy(x => x.OldListRecord?.ZipName?.Length)?.OldListRecord?.ZipName?.Length
                ?? 3;
            var padRightStatus = nameof(DiffType.Modified).Length;

            _logger.Information("Updates available: {NumberUpdates}", diffs.Count);

            foreach (var diff in diffs)
            {
                if (diff.DiffType == DiffType.Modified)
                {
                    _logger.Information(
                        "{Status}: {AddonName} {OldZipName} -> {NewZipName}",
                        diff.DiffType.ToString().PadRight(padRightStatus),
                        diff.OldListRecord!.AddonName!.PadRight(padRightAddonName),
                        diff.OldListRecord.ZipName!.PadRight(padRightOldZipName),
                        diff.NewListRecord!.ZipName
                    );
                }
                else
                {
                    _logger.Information(
                        "{Status}: {AddonName} {OldZipName} -> {NewZipName}",
                        diff.DiffType.ToString().PadRight(padRightStatus),
                        diff.DiffType switch
                        {
                            DiffType.Added =>
                                $"{diff.NewListRecord?.AddonName ?? diff.NewListRecord?.DlLink ?? "N/A"}".PadRight(
                                    padRightAddonName
                                ),
                            DiffType.Removed => diff.OldListRecord?.AddonName?.PadRight(
                                padRightAddonName
                            ),
                            _ => throw new ArgumentOutOfRangeException(),
                        },
                        $"{diff.OldListRecord?.ZipName ?? "N/A"}".PadRight(padRightOldZipName),
                        $"{diff.NewListRecord?.ZipName ?? "N/A"}"
                    );
                }
            }
        }
        else
        {
            _logger.Information("No updates found");
        }
    }

    private readonly ILogger _logger = logger;
    private readonly CliSettings _cliSettings = cliSettings;
    private readonly IGetStalkerModsFromApi _getStalkerModsFromApi = getStalkerModsFromApi;
    private readonly IModListRecordFactory _modListRecordFactory = modListRecordFactory;
}
