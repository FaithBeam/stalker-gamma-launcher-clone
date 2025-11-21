using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Factories;

public class ModDownloadExtractProgressVmFactory
{
    public ModDownloadExtractProgressVm Create(ModListRecord modListRecord) => new(modListRecord);
}
