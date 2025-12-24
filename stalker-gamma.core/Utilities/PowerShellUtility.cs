using System.Diagnostics;

namespace stalker_gamma.core.Utilities;

public static class PowerShellUtility
{
    public static void Execute(string command)
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
