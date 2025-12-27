using stalker_gamma.core.Models;

namespace stalker_gamma.core.Factories;

public static class ModListRecordFactory
{
    public static IModListRecord Create(string line, int idx)
    {
        idx++;
        var lineSplit = line.Split('\t');
        var dlLink = lineSplit[0];

        var instructions = lineSplit.ElementAtOrDefault(1);
        var patch = lineSplit.ElementAtOrDefault(2);
        var addonName = lineSplit.ElementAtOrDefault(3)?.TrimStart();
        var modDbUrl = lineSplit.ElementAtOrDefault(4);
        var zipName = lineSplit.ElementAtOrDefault(5);
        var md5ModDb = lineSplit.ElementAtOrDefault(6);

        if (lineSplit.Length == 1)
        {
            return new Separator { DlLink = dlLink, Counter = idx };
        }

        if (dlLink.Contains("moddb"))
        {
            return new ModDbRecord
            {
                Counter = idx,
                DlLink = dlLink,
                Instructions = instructions,
                Patch = patch,
                AddonName = addonName,
                ModDbUrl = modDbUrl,
                ZipName = zipName,
                Md5ModDb = md5ModDb,
            };
        }

        if (dlLink.Contains("github"))
        {
            return new GithubRecord
            {
                Counter = idx,
                DlLink = dlLink,
                Instructions = instructions,
                Patch = patch,
                AddonName = addonName,
                ModDbUrl = modDbUrl,
                ZipName = $"{dlLink.Split('/')[4]}.zip",
                Md5ModDb = md5ModDb,
            };
        }

        if (dlLink.Contains("gamma_large_files"))
        {
            return new GammaLargeFile
            {
                Counter = idx,
                DlLink = dlLink,
                Instructions = instructions,
                Patch = patch,
                AddonName = addonName,
                ModDbUrl = modDbUrl,
                ZipName = zipName,
                Md5ModDb = md5ModDb,
            };
        }

        throw new Exception($"Invalid mod list record: {line}");
    }
}
