using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    private Texture2D ScreenOverlayRectangle;
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

    public void LoadContent(GraphicsDevice device, IAudioService audioService)
    {
        // BACKGROUND
        _menuBackground = new();
        _menuBackground = Globals.UI.CreateSprite("menu-background");

        // CREATE ALL MENUS
        mainMenu = new();
        mainMenu.LoadContent(device);

        settingsMenu = new();
        settingsMenu.LoadContent(device);
    
        deathScreen = new();
        deathScreen.LoadContent(device);

        pauseScreen = new();
        pauseScreen.LoadContent(device);

        gamePlayUI = new();
    
        // Gift Card
        GiftCard = new();
        GiftCard.LoadContent(device);

        // Screen Overlay
        ScreenOverlayRectangle = new Texture2D(device, 1, 1);
        ScreenOverlayRectangle.SetData([Color.White]); // Set Screen overlay rectangle to White
    }

    public void Update(IAudioService audio)
    {
        if (Globals.SETTINGS_MENU)
        {
            desiredCursor = settingsMenu.Update(audio);
            Mouse.SetCursor(desiredCursor);
            return;
        }

        if (Globals.MENU)
        {
            desiredCursor = mainMenu.Update(audio);
            Mouse.SetCursor(desiredCursor);
            return;
        }

        if (Globals.DISPLAY_PAUSE_MENU)
        {
            desiredCursor = pauseScreen.Update(audio);
            Mouse.SetCursor(desiredCursor);
            return;
        }

        // IF PLAYER IS DEAD
        if (!Globals.PLAYER_ALIVE)
        {
            desiredCursor = deathScreen.Update(audio);
            Mouse.SetCursor(desiredCursor);
            return;
        }

        // PLAYER IS ALIVE
        desiredCursor = GiftCard.Update(audio);
        Mouse.SetCursor(desiredCursor);
    }

    public void Draw(IPlayer player)
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
            gamePlayUI.Draw(player);

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
    }

    private void DrawOverlay()
    {
        Globals.SpriteBatch.Draw(
            ScreenOverlayRectangle,
            new Rectangle(0, 0, Globals.VIRTUAL_WIDTH, Globals.VIRTUAL_HEIGHT),
            Globals.ScreenOverlayColor
        );
    }
}