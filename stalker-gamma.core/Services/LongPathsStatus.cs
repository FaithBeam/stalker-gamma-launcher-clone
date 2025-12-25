using Microsoft.Win32;

namespace stalker_gamma.core.Services;

public interface IILongPathsStatusService
{
    public bool? Execute();
}

public static class LongPathsStatus
{
    public sealed class Handler(IIsRanWithWineService isRanWithWineService)
        : IILongPathsStatusService
    {
        public bool? Execute()
        {
            if (!OperatingSystem.IsWindows() || isRanWithWineService.IsRanWithWine())
            {
                return null;
            }

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\FileSystem"
                );
                return key?.GetValue("LongPathsEnabled", 0) as int? == 1;
            }
            catch (Exception ex)
            {
                throw new LongPathsStatusException("Error retrieving long paths", ex);
            }
        }
    }
}

public class LongPathsStatusException(string message, Exception innerException)
    : Exception(message, innerException);
