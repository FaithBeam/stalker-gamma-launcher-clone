using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;

public static class GetLocalMods
{
    public sealed class Handler(ModListRecordFactory modListRecordFactory)
    {
        private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

        public async Task<IList<ModListRecord>> ExecuteAsync()
        {
            var modListFile = Path.Join(_dir, "mods.txt");
            if (!File.Exists(modListFile))
            {
                return [];
            }

            return (await File.ReadAllLinesAsync(modListFile))
                .Select(modListRecordFactory.Create)
                .Where(x => x is ModListRecord)
                .Cast<ModListRecord>()
                .ToList();
        }
    }
}
