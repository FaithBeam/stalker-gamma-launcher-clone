using ReactiveUI;

namespace stalker_gamma_gui.ViewModels.Tabs.ModDbUpdatesTab;

public enum UpdateType
{
    None,
    Add,
    Remove,
    Update,
}

public class UpdateableModVm(
    string addonName,
    string url,
    string? localVersion,
    string? remoteVersion,
    UpdateType updateType
) : ViewModelBase
{
    public UpdateType UpdateType
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = updateType;

    public string AddonName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = addonName;

    public string? LocalVersion
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = localVersion;

    public string? RemoteVersion
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = remoteVersion;

    public string Url
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = url;
}
