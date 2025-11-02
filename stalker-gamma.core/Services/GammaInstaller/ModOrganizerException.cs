namespace stalker_gamma.core.Services.GammaInstaller;

public class ModOrganizerServiceException : Exception
{
    public ModOrganizerServiceException(string message)
        : base(message) { }

    public ModOrganizerServiceException(string msg, Exception innerException)
        : base(msg, innerException) { }
}
