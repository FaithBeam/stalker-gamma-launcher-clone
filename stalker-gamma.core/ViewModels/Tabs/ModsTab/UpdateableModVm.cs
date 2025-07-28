using ReactiveUI;

namespace stalker_gamma.core.ViewModels.Tabs.ModsTab;

public class UpdateableModVm(
    string addonName,
    string url,
    string? localVersion,
    string? remoteVersion
) : ViewModelBase
{
    private string _addonName = addonName;
    private string _url = url;
    private string? _localVersion = localVersion;
    private string? _remoteVersion = remoteVersion;

    public string AddonName
    {
        get => _addonName;
        set => this.RaiseAndSetIfChanged(ref _addonName, value);
    }

    public string? LocalVersion
    {
        get => _localVersion;
        set => this.RaiseAndSetIfChanged(ref _localVersion, value);
    }

    public string? RemoteVersion
    {
        get => _remoteVersion;
        set => this.RaiseAndSetIfChanged(ref _remoteVersion, value);
    }

    public string Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value);
    }
}
