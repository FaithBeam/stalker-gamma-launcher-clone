using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using stalker_gamma.core.ViewModels.Tabs.ModListTab;

namespace stalker_gamma_gui.Controls.Tabs;

public partial class ModListTab : ReactiveUserControl<ModListTabVm>
{
    public ModListTab()
    {
        InitializeComponent();

        this.WhenActivated(d => { });
    }
}
