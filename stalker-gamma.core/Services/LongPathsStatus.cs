using Microsoft.Win32;

namespace stalker_gamma.core.Services;

public interface IILongPathsStatusService
{
    public bool? Execute();
}

public static class LongPathsStatus
{
    public sealed class Handler(
        IIsRanWithWineService isRanWithWineService,
        IOperatingSystemService operatingSystemService
    ) : IILongPathsStatusService
    {
        private readonly IIsRanWithWineService _isRanWithWineService = isRanWithWineService;
        private readonly IOperatingSystemService _operatingSystemService = operatingSystemService;

        public bool? Execute()
        {
            if (!_operatingSystemService.IsWindows() || _isRanWithWineService.IsRanWithWine())
            {
                return null;
            }

            try
            {
#pragma warning disable CA1416
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\FileSystem"
                );
                return key?.GetValue("LongPathsEnabled", 0) as int? == 1;
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                throw new LongPathsStatusException("Error retrieving long paths", ex);
            }
        }
    }
}

public class LongPathsStatusException : Exception
{
    public LongPathsStatusException(string message)
        : base(message) { }

    public LongPathsStatusException(string message, Exception innerException)
        : base(message, innerException) { }
}
