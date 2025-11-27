using System.Runtime.Serialization;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Enums;

public enum InstallType
{
    [EnumMember(Value = "Full Install")]
    FullInstall,

    [EnumMember(Value = "Update")]
    Update,
}
