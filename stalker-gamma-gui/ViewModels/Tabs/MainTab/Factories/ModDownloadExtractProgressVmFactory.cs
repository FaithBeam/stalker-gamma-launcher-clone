using stalker_gamma.core.Models;

namespace stalker_gamma_gui.ViewModels.Tabs.MainTab.Factories;

public class ModDownloadExtractProgressVmFactory
{
    public ModDownloadExtractProgressVm Create(ModListRecord modListRecord) => new(modListRecord);
}
