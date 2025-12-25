using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using stalker_gamma.core.Factories;
using stalker_gamma.core.Models;

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
                .Select((line, idx) => modListRecordFactory.Create(line, idx))
                .Where(x => x is ModListRecord)
                .Cast<ModListRecord>()
                .ToList();
        }
    }
}
