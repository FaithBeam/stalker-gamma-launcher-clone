using System.Collections.ObjectModel;

namespace stalker_gamma.core.ViewModels.Tabs.ModListTab;

public class ModNode
{
    public ObservableCollection<ModNode>? SubNodes { get; }
    public string Title { get; }
    public bool Enabled { get; }
    public bool Separator { get; }

    public ModNode(string title, bool enabled, bool separator)
    {
        Title = title;
        Enabled = enabled;
        Separator = separator;
    }

    public ModNode(
        string title,
        bool enabled,
        bool separator,
        ObservableCollection<ModNode> subNodes
    )
    {
        Title = title;
        Enabled = enabled;
        SubNodes = subNodes;
        Separator = separator;
    }
}
