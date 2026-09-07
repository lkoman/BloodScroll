using System;
using System.IO;
using System.Text.Json;
using MonoGameLibrary;
using System.Collections.Generic;


namespace BloodScroll;

public static class SaveManager
{
    // WHERE THE SAVE LIVES
    //
    // In the player's own AppData, NOT next to the game. A relative path is
    // resolved against the working directory, which is only the game folder
    // when the game is launched from it - a shortcut or a launcher sets a
    // different one, and the save silently lands somewhere else. An installed
    // copy may also sit in a folder the player is not allowed to write to.
    private static string SavePath
    {
        get
        {
            var dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BloodScroll");

            Directory.CreateDirectory(dir);

            return System.IO.Path.Combine(dir, "settings.json");
        }
    }

    // Where the save USED to live. Read once, so a player who already has
    // scores keeps them; the next Save writes to the new place.
    private static string LegacyPath => System.IO.Path.Combine("Content", "settings.json");

    public static void Load()
    {
        // A save is never worth taking the game down for. Missing, unreadable
        // or half-written, the answer is the same: start on zeroes.
        try
        {
            var path = File.Exists(SavePath) ? SavePath
                     : File.Exists(LegacyPath) ? LegacyPath
                     : null;

            if (path == null)
                return;

            var data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path));

            if (data != null && data.Gameplay.Count > 0)
                Globals.HIGH_SCORE = data.Gameplay[0].HighScore;
        }
        catch (Exception)
        {
            // Leaves HIGH_SCORE at its default
        }
    }

    public static void Save()
    {
        var data = new SaveData
        {
            Gameplay = [new HighScoreEntry { HighScore = Globals.HIGH_SCORE }]
        };

        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SavePath, json);
        }
        catch (Exception)
        {
            // A high score that cannot be written down is not a reason to
            // crash on the death screen. The run still counts in memory.
        }
    }

    // The shape of settings.json. Gameplay is a LIST holding one entry rather
    // than a plain field because that is the shape already written to disk -
    // flattening it would make every existing save unreadable and wipe the
    // high scores.
    private class SaveData
    {
        public List<HighScoreEntry> Gameplay { get; set; } = [];
    }

    private class HighScoreEntry
    {
        public int[] HighScore { get; set; } = [0, 0, 0];
    }
}
