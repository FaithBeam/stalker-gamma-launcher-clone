namespace stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab;

public record GammaUpdateRecord(string Path, GammaUpdateType UpdateType);

public enum GammaUpdateType
{
    Modified,
    Added,
    Removed,
}
