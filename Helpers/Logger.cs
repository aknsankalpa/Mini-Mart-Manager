using System.IO;

namespace RetailFlow.Helpers;

/// <summary>
/// Minimal file-based logging for unexpected errors. The UI only ever shows a
/// plain-language message (see App.xaml.cs's global exception handler and the try/catch
/// blocks in the services) — this is where the actual exception details go instead, so a
/// developer can still diagnose a problem after the fact.
/// </summary>
public static class Logger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "retailflow-error.log");

    public static void LogError(string context, Exception ex)
    {
        try
        {
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(LogPath, entry);
        }
        catch
        {
            // Logging must never itself crash the app or surface a new error to the user —
            // if the log file can't be written (e.g. a read-only folder), just move on.
        }
    }
}
