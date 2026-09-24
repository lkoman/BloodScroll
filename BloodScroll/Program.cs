// THE SAVE IS READ BEFORE THERE IS A WINDOW.
//
// Core builds the window straight out of Globals.FULLSCREEN in its constructor,
// so a save read any later than this would open the game the wrong way round and
// then correct itself on screen. Everything else it restores is static state and
// does not care when it arrives.
BloodScroll.SaveManager.Load();

using var game = new BloodScroll.BloodScroll();
game.Run();
