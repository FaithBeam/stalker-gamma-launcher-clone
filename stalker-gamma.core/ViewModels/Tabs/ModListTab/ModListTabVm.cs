using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using DynamicData;
using ReactiveUI;
using stalker_gamma.core.Services;

namespace stalker_gamma.core.ViewModels.Tabs.ModListTab;

public class ModListTabVm : ViewModelBase, IActivatableViewModel
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public ModListTabVm(ProgressService progressService)
    {
        Activator = new ViewModelActivator();
        var mo2ModsFile = Path.Join(_dir, "..", "profiles", "G.A.M.M.A", "modlist.txt");
        GetModListCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            if (!File.Exists(mo2ModsFile))
            {
                progressService.UpdateProgress($"Mods list file not found: {mo2ModsFile}");
                return;
            }

            var modsList = (await File.ReadAllLinesAsync(mo2ModsFile)).Where(x =>
                x.StartsWith('-') || x.StartsWith('+')
            );
            ModsList = [];
            foreach (var mod in modsList) { }
        });

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                GetModListCmd.Execute().Subscribe();
            }
        );
    }

    public ReactiveCommand<Unit, Unit> GetModListCmd { get; }

    public ObservableCollection<ModNode> ModsList { get; set; }
    public ViewModelActivator Activator { get; }
}
