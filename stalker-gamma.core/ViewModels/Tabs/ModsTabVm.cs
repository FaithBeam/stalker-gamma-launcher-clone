using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs;

public class ModsTabVm : ViewModelBase
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public ModsTabVm(ModDb modDb)
    {
        var modListFile = Path.Join(_dir, "mods.txt");

        var files = File.ReadAllLines(modListFile)
            .Select(x => ParseModListRecord.ParseLine(x, modDb))
            .ToList();
        files.First()
    }
}
