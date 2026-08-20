using System.IO;
using System.Text.Json;
using MonoGameLibrary;
using System.Collections.Generic;


namespace BloodScroll;

public static class SaveManager
{
    private static string Path => System.IO.Path.Combine("Content", "settings.json");

    public static void Load()
    {
        if (!File.Exists(Path))
            return;

        var json = File.ReadAllText(Path);
        var data = JsonSerializer.Deserialize<SaveData>(json);

        if (data != null)
        {
            // GAMEPLAY
            if (data.Gameplay.Count > 0)
                Globals.HIGH_SCORE = data.Gameplay[0].HighScore;

            // SETTINGS
            /*foreach (var setting in data.Settings)
            {
                if (setting.Key == "fullscreen" && setting.BoolValue is bool fs)
                    Globals.FULLSCREEN = fs;
            }*/
        }
    }
    public static void Save()
    {
        var data = new SaveData
        {
            Gameplay = new List<HighScoreEntry>
            {
                new HighScoreEntry { HighScore = Globals.HIGH_SCORE }
            },
            /*Settings = new List<SettingEntry>
            {
                new SettingEntry { Key = "fullscreen", BoolValue = Globals.FULLSCREEN }
            }*/
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path, json);
    }
    private class SaveData
    {
        public List<HighScoreEntry> Gameplay { get; set; } = new();
        //public List<SettingEntry> Settings { get; set; } = new();
    }

    private class HighScoreEntry
    {
        public int[] HighScore { get; set; } = [0, 0, 0];
    }

    private class SettingEntry
    {
        public string Key { get; set; } = "";
        public bool BoolValue { get; set; }
    }
}
