using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

public class UI : IUI
{
    // MENUS
    private MainMenu mainMenu;
    private SettingsMenu settingsMenu;
    private DeathScreen deathScreen;
    private PauseScreen pauseScreen;
    private GamePlayUI gamePlayUI;

    // VARIABLES
    private Sprite _menuBackground;
    private MouseCursor desiredCursor = MouseCursor.Arrow;

    // Gift Card
    private Card GiftCard;
    public bool GiftCardDisplayed
    {
        get => GiftCard.IsVisible;
        set => GiftCard.IsVisible = value;
    }
    public string GiftTitle
    {
        get => GiftCard.GiftText;
        set => GiftCard.GiftText = value;
    }

    public void LoadContent()
    {
        // BACKGROUND
        _menuBackground = Globals.UI.CreateSprite("menu-background");

        // CREATE ALL MENUS
        mainMenu = new();
        mainMenu.LoadContent();

        settingsMenu = new();
        settingsMenu.LoadContent();

        deathScreen = new();
        deathScreen.LoadContent();

        pauseScreen = new();
        pauseScreen.LoadContent();

        gamePlayUI = new();

        // Gift Card
        GiftCard = new();
        GiftCard.LoadContent();
    }

    // WHICHEVER SCREEN IS IN FRONT GETS THE MOUSE, and it is the only one that
    // updates. Each of them answers with the cursor it wants, so the choice of
    // screen and the setting of the cursor stay one line apart instead of being
    // repeated five times.
    // A fresh run: the HUD forgets which score landmarks have been passed
    public void Restart(IAudioService audio)
    {
        gamePlayUI.Restart(audio);
    }

    public void Update(IAudioService audio)
    {
        //
        // THE HUD TICKS WHATEVER SCREEN IS IN FRONT
        //
        // Outside the chain below, because it is not a screen competing for the
        // mouse - it is the score's own clock. The biggest landmark of a run
        // arrives on the frame the hive doubles the score, which is the same
        // frame the gift card goes up and pauses everything, so a HUD that only
        // ticked while the playfield was live would miss its own best moment.
        //
        if (!Globals.MENU)
            gamePlayUI.Update(audio);

        if (Globals.SETTINGS_MENU)
            desiredCursor = settingsMenu.Update(audio);

        else if (Globals.MENU)
            desiredCursor = mainMenu.Update(audio);

        else if (Globals.DISPLAY_PAUSE_MENU)
            desiredCursor = pauseScreen.Update(audio);

        else if (!Globals.PLAYER_ALIVE)
            desiredCursor = deathScreen.Update(audio);

        // PLAYER IS ALIVE
        else
            desiredCursor = GiftCard.Update(audio);

        Mouse.SetCursor(desiredCursor);
    }

    public void Draw(IPlayer player, IWeaponsManager weapons = null)
    {
        Globals.SpriteBatch.Begin();

        DrawOverlay();

        if (Globals.SETTINGS_MENU)
        {
            DrawMenuBackground();
            settingsMenu.Draw();
        }
        
        else if (Globals.MENU)
        {
            DrawMenuBackground();
            mainMenu.Draw();
        }

        else if (Globals.PLAYER_ALIVE)
        {
            gamePlayUI.Draw(player, weapons);

            if (Globals.DISPLAY_PAUSE_MENU)
                pauseScreen.Draw();
            
            else if (GiftCard.IsVisible)
                GiftCard.Draw();

        }
        else
            deathScreen.Draw();

        Globals.SpriteBatch.End();
    }

    private void DrawMenuBackground()
    {
        _menuBackground.Draw();
        DrawOverlay();
        DrawVignette();
    }

    // The whole screen washed in whatever the game state calls for - black
    // while paused, dark red on death. RoundedRect owns the white pixel this
    // is stretched from, so the UI no longer keeps one of its own.
    private static void DrawOverlay()
    {
        RoundedRect.Rect(
            new Rectangle(0, 0, Globals.VIRTUAL_WIDTH, Globals.VIRTUAL_HEIGHT),
            Globals.ScreenOverlayColor);
    }

    //
    // THE CORNERS OF THE MENU, PULLED DOWN
    //
    // The drawn background is busy right to the edges and the panel sits in the
    // middle of it, so without this the eye has nowhere to settle. A handful of
    // black bands thickening towards each edge darkens the outside of the frame
    // and leaves the middle where it was - the panel comes forward on its own,
    // without the background having to be dimmed flat and lost.
    //
    private static void DrawVignette()
    {
        const int BANDS = 24;
        const int DEPTH = 300;
        const float STRENGTH = 0.55f;

        int step = DEPTH / BANDS;

        // Band 0 is hard against the screen edge and darkest; each one after it
        // sits a step further in and is lighter, fading to nothing by DEPTH.
        // The bands do not overlap, so the alpha here is the alpha on screen.
        for (int i = 0; i < BANDS; i++)
        {
            float t = 1f - i / (float)BANDS;
            Color shade = Color.Black * (STRENGTH * t * t);

            int inset = i * step;

            RoundedRect.Rect(new Rectangle(0, inset, Globals.VIRTUAL_WIDTH, step), shade);
            RoundedRect.Rect(new Rectangle(0, Globals.VIRTUAL_HEIGHT - inset - step, Globals.VIRTUAL_WIDTH, step), shade);
            RoundedRect.Rect(new Rectangle(inset, 0, step, Globals.VIRTUAL_HEIGHT), shade);
            RoundedRect.Rect(new Rectangle(Globals.VIRTUAL_WIDTH - inset - step, 0, step, Globals.VIRTUAL_HEIGHT), shade);
        }
    }
}