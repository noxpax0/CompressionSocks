using System.Text.Json;

namespace CompressionGarmentOrder;

internal sealed record WindowSettings(int X, int Y, int Width, int Height, bool Maximized)
{
    private static string PathName => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mercury", "window.json");

    public static WindowSettings? Load()
    {
        try { return JsonSerializer.Deserialize<WindowSettings>(File.ReadAllText(PathName)); }
        catch { return null; }
    }

    public void Save()
    {
        string directory = Path.GetDirectoryName(PathName)!;
        Directory.CreateDirectory(directory);
        string temporary = PathName + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this));
        File.Move(temporary, PathName, true);
    }

    public bool IsVisibleOnAnyScreen()
    {
        var bounds = new Rectangle(X, Y, Width, Height);
        return Screen.AllScreens.Any(screen => Rectangle.Intersect(screen.WorkingArea, bounds).Width >= 200 &&
                                               Rectangle.Intersect(screen.WorkingArea, bounds).Height >= 120);
    }
}
