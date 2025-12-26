using Avalonia.ReactiveUI;
using stalker_gamma_gui.ViewModels.Tabs.ModDbUpdatesTab;

namespace stalker_gamma_gui.Controls.Tabs;

public partial class ModDbUpdatesTab : ReactiveUserControl<ModDbUpdatesTabVm>
{
    public ModDbUpdatesTab()
    {
        InitializeComponent();
    }
}
