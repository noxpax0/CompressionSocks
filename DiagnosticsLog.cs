using System.Text;

namespace CompressionGarmentOrder;

internal static class DiagnosticsLog
{
    private static readonly object Gate = new();
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mercury", "Logs");
    private static readonly string LogPath = Path.Combine(DirectoryPath, "mercury.log");

    public static void Write(string area, Exception exception)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(DirectoryPath);
                RotateIfNeeded();
                string line = $"{DateTimeOffset.UtcNow:O}\t{Sanitize(area)}\t{exception.GetType().Name}\t{Sanitize(exception.Message)}{Environment.NewLine}";
                File.AppendAllText(LogPath, line, Encoding.UTF8);
            }
        }
        catch { }
    }

    public static void Info(string area, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(DirectoryPath);
                RotateIfNeeded();
                File.AppendAllText(LogPath,
                    $"{DateTimeOffset.UtcNow:O}\t{Sanitize(area)}\tINFO\t{Sanitize(message)}{Environment.NewLine}", Encoding.UTF8);
            }
        }
        catch { }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(LogPath) || new FileInfo(LogPath).Length < 1_000_000) return;
        File.Move(LogPath, Path.Combine(DirectoryPath, "mercury.previous.log"), true);
    }

    private static string Sanitize(string value) => value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
}
