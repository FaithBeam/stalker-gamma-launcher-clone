using System.Text;

namespace stalker_gamma.core.Models;

public class GlobalSettings
{
    public bool UseCurlImpersonate { get; set; }
    public StringBuilder Logs { get; } = new();
}
