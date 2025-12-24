using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Commands;

public static class AddFoldersToWinDefenderExclusion
{
    public sealed record Command(params string[] Folders);

    public sealed class Handler
    {
        public void Execute(Command c)
        {
            var command =
                "Add-MpPreference -ExclusionPath "
                + string.Join(',', c.Folders.Select(x => $"'{x}'"));
            PowerShellUtility.Execute(command);
        }
    }
}
