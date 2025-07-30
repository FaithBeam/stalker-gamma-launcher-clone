using System.Collections.ObjectModel;

namespace stalker_gamma.core.ViewModels.Tabs.ModListTab;

public class ModNode
{
    public ObservableCollection<ModNode>? SubNodes { get; }
    public string Title { get; }

    public ModNode(string title)
    {
        Title = title;
    }

    public ModNode(string title, ObservableCollection<ModNode> subNodes)
    {
        Title = title;
        SubNodes = subNodes;
    }
}
