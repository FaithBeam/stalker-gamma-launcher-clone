namespace stalker_gamma.core.ViewModels.Tabs.GammaUpdatesTab.Models;

public enum GitDiffType
{
    Modified,
    Added,
    Deleted,
    Renamed,
}

public record GitDiff(GitDiffType Type, string Path);
