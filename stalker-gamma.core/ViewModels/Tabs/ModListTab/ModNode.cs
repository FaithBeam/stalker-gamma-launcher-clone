using System.Collections.ObjectModel;
using ReactiveUI;

namespace stalker_gamma.core.ViewModels.Tabs.ModListTab;

public class ModNode : ReactiveObject
{
    private string _title;
    private bool _enabled;
    private bool _separator;
    public ObservableCollection<ModNode>? SubNodes { get; }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => this.RaiseAndSetIfChanged(ref _enabled, value);
    }

    public bool Separator
    {
        get => _separator;
        set => this.RaiseAndSetIfChanged(ref _separator, value);
    }

    public ModNode(string title, bool enabled, bool separator)
    {
        _title = title;
        _enabled = enabled;
        _separator = separator;
    }

    public ModNode(
        string title,
        bool enabled,
        bool separator,
        ObservableCollection<ModNode> subNodes
    )
    {
        _title = title;
        _enabled = enabled;
        SubNodes = subNodes;
        _separator = separator;
    }

    public override string ToString()
    {
        if (SubNodes is not null && SubNodes.Any())
        {
            return $"{(SubNodes?.Any() ?? false ? string.Join("\n", SubNodes.Reverse().Select(x => x.ToString())) : string.Empty)}"
                + "\n"
                + $"{(Enabled ? "+" : "-")}{Title}{(Separator ? "_separator" : "")}";
        }
        return $"{(Enabled ? "+" : "-")}{Title}{(Separator ? "_separator" : "")}";
    }
}
