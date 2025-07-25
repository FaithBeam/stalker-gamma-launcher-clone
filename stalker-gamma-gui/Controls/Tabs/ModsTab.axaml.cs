using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using stalker_gamma.core.ViewModels.Tabs;

namespace stalker_gamma_gui.Controls.Tabs;

public partial class ModsTab : ReactiveUserControl<ModsTabVm>
{
    public ModsTab()
    {
        InitializeComponent();
    }
}
