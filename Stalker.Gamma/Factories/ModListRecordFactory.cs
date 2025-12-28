using Stalker.Gamma.Models;

namespace Stalker.Gamma.Factories;

public interface IModListRecordFactory
{
    List<ModListRecord> Create(string modListTxt);
}

public class ModListRecordFactory : IModListRecordFactory
{
    public List<ModListRecord> Create(string modListTxt) =>
        modListTxt
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var lineSplit = line.Split('\t');
                return new ModListRecord
                {
                    DlLink = lineSplit[0].Trim(),
                    Instructions = lineSplit.ElementAtOrDefault(1)?.Trim(),
                    Patch = lineSplit.ElementAtOrDefault(2)?.Trim(),
                    AddonName = lineSplit.ElementAtOrDefault(3)?.Trim(),
                    ModDbUrl = lineSplit.ElementAtOrDefault(4)?.Trim(),
                    ZipName = lineSplit.ElementAtOrDefault(5)?.Trim(),
                    Md5ModDb = lineSplit.ElementAtOrDefault(6)?.Trim(),
                };
            })
            .ToList();
}
