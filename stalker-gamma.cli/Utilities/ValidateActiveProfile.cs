using Serilog;
using stalker_gamma.cli.Models;

namespace stalker_gamma.cli.Utilities;

public static class ValidateActiveProfile
{
    public static void Validate(ILogger logger, CliProfile? profile)
    {
        if (profile is not null)
        {
            return;
        }
        logger.Error(
            "No active profile, create one with config create or select one with config use"
        );
        Environment.Exit(1);
    }
}
