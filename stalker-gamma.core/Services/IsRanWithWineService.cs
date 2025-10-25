using Microsoft.Win32;

namespace stalker_gamma.core.Services;

public interface IIsRanWithWineService
{
    bool IsRanWithWine();
}

public class IsRanWithWineService(IOperatingSystemService operatingSystemService)
    : IIsRanWithWineService
{
    private readonly IOperatingSystemService _operatingSystemService = operatingSystemService;

    public bool IsRanWithWine()
    {
        try
        {
            if (_operatingSystemService.IsWindows())
            {
#pragma warning disable CA1416
                using var key = Registry.CurrentUser.OpenSubKey("Software\\Wine");
#pragma warning restore CA1416
                if (key is not null)
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }
}
