namespace stalker_gamma.core.ViewModels.Tabs.BackupTab;

public record RestoreCommand(string ArchivePath, string DestinationFolder);

public class RestoreService
{
    public void Restore(RestoreCommand command) { }
}
