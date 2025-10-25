using System.Diagnostics;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Commands;

public static class EnableLongPathsOnWindows
{
    public sealed class Handler
    {
        public void Execute()
        {
            const string command = """
                Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value "1"
                """;
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
