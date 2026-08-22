using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

public class PauseScreen
{
    // BUTTONS
    private Button ButtonRestart, ButtonResume, ButtonMenu;

    private static Vector2 GamePausedPos;

    public void LoadContent(GraphicsDevice device)
    {
        //
        // POSITIONS FOR UI ELEMENTS
        //

        GamePausedPos = new Vector2(
            Globals.VIRTUAL_WIDTH / 2 - UISettings.fontBig.MeasureString("Game Paused").X / 2,
            Globals.VIRTUAL_HEIGHT / 2 - UISettings.fontBig.MeasureString("Game Paused").Y - UISettings.buttonSize.Y
        );

        //
        // BUTTONS
        //
        // Main Menu buttons

        // Pause and Death Menu buttons
        ButtonRestart = new Button();
        ButtonRestart.LoadContent("RESTART", UISettings.buttonSize, device);

        ButtonResume = new Button();
        ButtonResume.LoadContent("RESUME", UISettings.buttonSize, device);

        // Shared or other buttons
        ButtonMenu = new Button();
        ButtonMenu.LoadContent("MENU", UISettings.buttonSize, device);
    }

    public MouseCursor Update(IAudioService audio)
    {
        MouseCursor desiredCursor;

        ButtonResume.SetOrder(0);
        ButtonRestart.SetOrder(1);
        ButtonMenu.SetOrder(2);

        ButtonRestart.UpdateHoverColor(audio);
        ButtonMenu.UpdateHoverColor(audio);
        ButtonResume.UpdateHoverColor(audio);

        if (ButtonRestart.Hover() || ButtonMenu.Hover() || ButtonResume.Hover()) {
            desiredCursor = MouseCursor.Hand;
        }
        else {
            desiredCursor = MouseCursor.Arrow;
        }

        if (ButtonRestart.ButtonClicked(audio))
        {
            Globals.RESTART = true;
            desiredCursor = MouseCursor.Arrow;
        }
        else if (ButtonMenu.ButtonClicked(audio))
        {
            Globals.MENU = true;
            desiredCursor = MouseCursor.Arrow;

            audio.SwitchToMenuMusic();
        }
        else if (ButtonResume.ButtonClicked(audio))
        {
            Globals.DISPLAY_PAUSE_MENU = false;
            Globals.PAUSE = false;
        }

        return desiredCursor;
    }

    public void Draw()
    {
        Globals.SpriteBatch.DrawString(UISettings.fontBig, "GAME PAUSED", GamePausedPos, Globals.Red);

        ButtonRestart.Draw(UISettings.buttonFont);
        ButtonMenu.Draw(UISettings.buttonFont);
        ButtonResume.Draw(UISettings.buttonFont);
    }
}