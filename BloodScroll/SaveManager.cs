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
    // scores keeps them; the next Save writes to the new place. Resolved against
    // the game folder rather than the working directory, same as everything else.
    private static string LegacyPath =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "Content", "settings.json");

    // One per difficulty - baby, medium, hell. Anything else on disk is a save
    // from another version or a hand edit, and indexing it by DIFFICULTY would
    // throw on the main menu before the player touched anything.
    private const int DIFFICULTY_COUNT = 3;

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

            if (data == null)
                return;

            if (data.Gameplay.Count > 0)
                Globals.HIGH_SCORE = Fit(data.Gameplay[0].HighScore);

            // Anything the file does not mention keeps the default it was born
            // with, so a save written before these existed still loads.
            if (data.Settings != null)
            {
                Globals.FULLSCREEN = data.Settings.FullScreen;
                Globals.MUTED = data.Settings.Muted;
                Globals.DIFFICULTY = Math.Clamp(data.Settings.Difficulty, 0, DIFFICULTY_COUNT - 1);
            }
        }
        catch (Exception)
        {
            // Leaves everything at its default
        }
    }

    // A short or missing array is padded, a long one is cut. Never null, and
    // never a length the difficulty cannot index.
    private static int[] Fit(int[] scores)
    {
        var fitted = new int[DIFFICULTY_COUNT];

        if (scores == null)
            return fitted;

        for (int i = 0; i < DIFFICULTY_COUNT && i < scores.Length; i++)
            fitted[i] = scores[i];

        return fitted;
    }

    public static void Save()
    {
        var data = new SaveData
        {
            Gameplay = [new HighScoreEntry { HighScore = Globals.HIGH_SCORE }],
            Settings = new SettingsEntry
            {
                FullScreen = Globals.FULLSCREEN,
                Muted = Globals.MUTED,
                Difficulty = Globals.DIFFICULTY
            }
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
    // high scores. Settings was added later and is absent from older saves,
    // which is why Load checks it for null rather than trusting it.
    private class SaveData
    {
        public List<HighScoreEntry> Gameplay { get; set; } = [];
        public SettingsEntry Settings { get; set; }
    }

    private class HighScoreEntry
    {
        public int[] HighScore { get; set; } = [0, 0, 0];
    }

    // What the settings menu and the difficulty button are holding when the
    // game is closed, so the next launch opens the way it was left.
    private class SettingsEntry
    {
        public bool FullScreen { get; set; } = true;
        public bool Muted { get; set; } = false;
        public int Difficulty { get; set; } = 1;
    }
}
