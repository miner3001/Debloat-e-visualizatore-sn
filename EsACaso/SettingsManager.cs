using System;
using System.Drawing;
using System.IO;

namespace EsACaso
{
    public static class SettingsManager
    {
        private static readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EsACaso", "settings.ini");

        public static Rectangle WindowBounds { get; set; } = Rectangle.Empty;
        public static bool DarkTheme { get; set; } = true;

        public static void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                var lines = File.ReadAllLines(_path);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length != 2) continue;
                    var key = parts[0].Trim();
                    var val = parts[1].Trim();
                    switch (key)
                    {
                        case "X": WindowBounds = new Rectangle(int.Parse(val), WindowBounds.Y, WindowBounds.Width, WindowBounds.Height); break;
                        case "Y": WindowBounds = new Rectangle(WindowBounds.X, int.Parse(val), WindowBounds.Width, WindowBounds.Height); break;
                        case "W": WindowBounds = new Rectangle(WindowBounds.X, WindowBounds.Y, int.Parse(val), WindowBounds.Height); break;
                        case "H": WindowBounds = new Rectangle(WindowBounds.X, WindowBounds.Y, WindowBounds.Width, int.Parse(val)); break;
                        case "Dark": DarkTheme = val == "1"; break;
                    }
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllLines(_path, new[]
                {
                    $"X={WindowBounds.X}",
                    $"Y={WindowBounds.Y}",
                    $"W={WindowBounds.Width}",
                    $"H={WindowBounds.Height}",
                    $"Dark={( DarkTheme ? "1" : "0")}"
                });
            }
            catch { }
        }
    }
}