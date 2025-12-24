using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services;

public class AddFoldersToWinDefenderExclusionService
{
    public void Execute(params string[] folders)
    {
        var command =
            "Add-MpPreference -ExclusionPath " + string.Join(',', folders.Select(x => $"'{x}'"));
        PowerShellUtility.Execute(command);
    }
}
