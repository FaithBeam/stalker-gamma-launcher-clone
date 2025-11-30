using System.Diagnostics;
using ConsoleAppFramework;

namespace stalker_gamma.updater.Commands;

[RegisterCommands]
public class CopyFiles
{
    /// <summary>
    /// Copies all files and directories from a source directory to a destination directory.
    /// </summary>
    /// <param name="destination">The destination directory where the files and subdirectories will be copied.</param>
    /// <param name="srcDir">The source directory to copy from. If null or empty, the current application base directory will be used as the source.</param>
    /// <returns>
    /// Returns an integer representing the operation status:
    /// 0 if the copying process completed successfully, 1 if an error occurred.
    /// </returns>
    public async Task<int> Copy(string destination, string? srcDir = null)
    {
        const string stalkerGammaProcessName = "stalker-gamma-gui.exe";

        srcDir = string.IsNullOrWhiteSpace(srcDir)
            ? Path.Join(Path.GetTempPath(), "stalker-gamma-updater")
            : srcDir;

        try
        {
            var procIdToKill = GetProcessIdByName(stalkerGammaProcessName);
            if (procIdToKill is > 0)
            {
                if (!KillProcessById(procIdToKill.Value))
                {
                    Environment.Exit(1);
                }
            }

            // Create destination directory if it doesn't exist
            Directory.CreateDirectory(destination);

            // Copy all files and subdirectories
            await CopyDirectoryAsync(srcDir, destination);

            Console.WriteLine($"Successfully copied all files from {srcDir} to {destination}");

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = Path.Join(destination, stalkerGammaProcessName),
                    UseShellExecute = true,
                    CreateNoWindow = true,
                }
            );

            return 0;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error copying files: {ex.Message}");
            return 1;
        }
    }

    private async Task CopyDirectoryAsync(string sourceDir, string destinationDir)
    {
        // Get all files in the source directory
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destinationDir, fileName);

            // Copy file asynchronously
            await using var sourceStream = new FileStream(
                file,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                true
            );
            await using var destinationStream = new FileStream(
                destFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                81920,
                true
            );
            await sourceStream.CopyToAsync(destinationStream);
        }

        // Recursively copy subdirectories
        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(directory);
            var destDir = Path.Combine(destinationDir, dirName);
            Directory.CreateDirectory(destDir);
            await CopyDirectoryAsync(directory, destDir);
        }
    }

    private static bool KillProcessById(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);

            if (process.HasExited)
            {
                return false;
            }
            process.Kill();
            process.WaitForExit();
            return true;
        }
        catch (ArgumentException)
        {
            // Process with the given ID doesn't exist
            return false;
        }
        catch (Exception ex)
        {
            // Handle other exceptions (e.g., access denied)
            Console.WriteLine($"Error killing process: {ex.Message}");
            return false;
        }
    }

    private static int? GetProcessIdByName(string processName)
    {
        try
        {
            // Remove .exe extension if present
            processName = processName.Replace(".exe", "");

            var processes = Process.GetProcessesByName(processName);

            if (processes.Length > 0)
            {
                // Return the first matching process ID
                return processes[0].Id;
            }

            return null; // No process found
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting process by name: {ex.Message}");
            return null;
        }
    }
}
