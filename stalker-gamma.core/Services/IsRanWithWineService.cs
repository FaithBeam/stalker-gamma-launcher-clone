using Microsoft.Win32;

namespace stalker_gamma.core.Services;

public interface IIsRanWithWineService
{
    bool IsRanWithWine();
}

public class IsRanWithWineService : IIsRanWithWineService
{
    public bool IsRanWithWine()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.CurrentUser.OpenSubKey("Software\\Wine");
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
