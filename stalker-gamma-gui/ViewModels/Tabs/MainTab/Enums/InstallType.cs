using System.Runtime.Serialization;

namespace stalker_gamma_gui.ViewModels.Tabs.MainTab.Enums;

public enum InstallType
{
    [EnumMember(Value = "Full Install")]
    FullInstall,

    [EnumMember(Value = "Update")]
    Update,
}
