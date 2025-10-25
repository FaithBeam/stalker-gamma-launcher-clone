using System.Diagnostics;

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
            ExecutePowerShellCommand(command);
        }
    }

    private static void ExecutePowerShellCommand(string command)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-Command \"{command}\"",
            UseShellExecute = true,
            CreateNoWindow = true,
            Verb = "runas", // Still need elevation
        };

        process.Start();
        process.WaitForExit();
    }
}
