namespace stalker_gamma.core.Services;

public enum OperatingSystemType
{
    Windows,
    Linux,

    // ReSharper disable once InconsistentNaming
    MacOS,
}

public interface IOperatingSystemService
{
    bool IsWindows();
    bool IsLinux();

    // ReSharper disable once InconsistentNaming
    bool IsMacOS();
}

public class OperatingSystemService : IOperatingSystemService
{
    public bool IsWindows() => OperatingSystem.IsWindows();

    public bool IsLinux() => OperatingSystemType == OperatingSystemType.Linux;

    public bool IsMacOS() => OperatingSystemType == OperatingSystemType.MacOS;

    public readonly OperatingSystemType OperatingSystemType =
        OperatingSystem.IsWindows() ? OperatingSystemType.Windows
        : OperatingSystem.IsLinux() ? OperatingSystemType.Linux
        : OperatingSystem.IsMacOS() ? OperatingSystemType.MacOS
        : throw new PlatformNotSupportedException();
}
