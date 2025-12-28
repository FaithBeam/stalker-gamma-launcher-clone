using Stalker.Gamma.Utilities;

namespace stalker_gamma.core.Services;

public class EnableLongPathsOnWindowsService
{
    public void Execute()
    {
        const string command = """
            Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value "1"
            """;
        PowerShellUtility.Execute(command);
    }
}
