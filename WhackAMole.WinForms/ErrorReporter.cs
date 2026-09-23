using System.Diagnostics;
using System.Text;

namespace WhackAMole;

public class ErrorReporter
{
    public void Record(string entry, Exception exception)
    {
        string line = $"{DateTimeOffset.Now:O} [{entry}] {exception}\n";
        try
        {
            string directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WhackAMole");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "errors.log"), line, Encoding.UTF8);
        }
        catch (Exception loggingFailure)
        {
            Trace.TraceError($"Could not write game error log: {loggingFailure}");
            Trace.TraceError(line);
        }
    }
}
