using Stalker.Gamma.Models;

namespace Stalker.Gamma.Factories;

public interface ISeparatorsFactory
{
    List<ISeparator> Create(IList<ModListRecord> records);
}

public class SeparatorsFactory : ISeparatorsFactory
{
    public List<ISeparator> Create(IList<ModListRecord> records) =>
        records
            .Where(r =>
                string.IsNullOrWhiteSpace(r.AddonName)
                && string.IsNullOrWhiteSpace(r.Instructions)
                && string.IsNullOrWhiteSpace(r.Md5ModDb)
                && string.IsNullOrWhiteSpace(r.ZipName)
                && string.IsNullOrWhiteSpace(r.ModDbUrl)
                && string.IsNullOrWhiteSpace(r.Patch)
            )
            .Select(
                (r, idx) =>
                    new Separator
                    {
                        Name = $"{r.DlLink} Separator",
                        FolderName = $"{++idx}- {r.DlLink}_separator",
                    }
            )
            .Cast<ISeparator>()
            .ToList();
}
