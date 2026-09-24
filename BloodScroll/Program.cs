// THE SAVE IS READ BEFORE THERE IS A WINDOW.
//
// Core builds the window straight out of Globals.FULLSCREEN in its constructor,
// so a save read any later than this would open the game the wrong way round and
// then correct itself on screen. Everything else it restores is static state and
// does not care when it arrives.
BloodScroll.SaveManager.Load();

// A CRASH LEAVES A NOTE BEHIND.
//
// Without this the window just vanishes and a player has nothing to send back.
// The log sits beside the save, in the player's own AppData, and is rethrown
// afterwards so the crash still behaves like a crash.
try
{
    using var game = new BloodScroll.BloodScroll();
    game.Run();
}
catch (System.Exception e)
{
    BloodScroll.SaveManager.WriteCrashLog(e);
    throw;
}
