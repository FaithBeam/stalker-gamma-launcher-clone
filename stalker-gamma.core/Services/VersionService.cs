using System.Reflection;

namespace stalker_gamma.core.Services;

public interface IVersionService
{
    string GetVersion();
}

public class VersionService : IVersionService
{
    private string? _version;

    public string GetVersion()
    {
        _version ??=
            Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? throw new VersionServiceException("Error getting version");
        return _version;
    }
}

public class VersionServiceException(string message) : Exception(message);
