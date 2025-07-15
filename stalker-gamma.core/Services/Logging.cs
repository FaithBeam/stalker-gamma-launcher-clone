using System.Text;
using ReactiveUI;

namespace stalker_gamma.core.Services;

public class Logging : ReactiveObject
{
    private readonly StringBuilder _logs = new();

    public string Logs
    {
        get => _logs.ToString();
        set
        {
            _logs.AppendLine(value);
            this.RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Logs a message.
    /// </summary>
    // public void LogWrite(string logstring) => Logs = logstring;
}
