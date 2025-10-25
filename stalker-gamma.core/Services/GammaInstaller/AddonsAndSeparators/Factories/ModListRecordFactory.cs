using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;

public class ModListRecordFactory(ModDb modDb, ICurlService curlService)
{
    private readonly ModDb _modDb = modDb;
    private readonly ICurlService _curlService = curlService;

    public IModListRecord Create(string line)
    {
        var lineSplit = line.Split('\t');
        var dlLink = lineSplit[0];

        var instructions = lineSplit.ElementAtOrDefault(1);
        var patch = lineSplit.ElementAtOrDefault(2);
        var addonName = lineSplit.ElementAtOrDefault(3);
        var modDbUrl = lineSplit.ElementAtOrDefault(4);
        var zipName = lineSplit.ElementAtOrDefault(5);
        var md5ModDb = lineSplit.ElementAtOrDefault(6);

        if (lineSplit.Length == 1)
        {
            return new Separator { DlLink = dlLink };
        }

        if (dlLink.Contains("moddb"))
        {
            return new ModDbRecord(_modDb, _curlService)
            {
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
            return new GithubRecord(_curlService)
            {
                DlLink = dlLink,
                Instructions = instructions,
                Patch = patch,
                AddonName = addonName,
                ModDbUrl = modDbUrl,
                ZipName = zipName,
                Md5ModDb = md5ModDb,
            };
        }

        if (dlLink.Contains("gamma_large_files"))
        {
            return new GammaLargeFile(_curlService)
            {
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
