using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

public class DeathScreen
{
    private static Vector2 YouDiedPos, YouDiedNotePos;
    private string YouDiedNote = "";

    private string OneLine = "-";

    // BUTTONS
    private Button ButtonRestart, ButtonMenu;


    public void LoadContent(GraphicsDevice device)
    {
        YouDiedPos = new Vector2(
            Globals.VIRTUAL_WIDTH / 2 - UISettings.fontBig.MeasureString("YOU DIED").X / 2,
            Globals.VIRTUAL_HEIGHT / 2 - UISettings.fontBig.MeasureString("YOU DIED").Y - UISettings.fontUI.MeasureString(OneLine).Y * 2
        );

        // Pause and Death Menu buttons
        ButtonRestart = new Button();
        ButtonRestart.LoadContent("RESTART", UISettings.buttonSize, device);

        ButtonMenu = new Button();
        ButtonMenu.LoadContent("MENU", UISettings.buttonSize, device);
    }

    public MouseCursor Update(IAudioService audio)
    {
        MouseCursor desiredCursor;

        if (Globals.HIGH_SCORE_THIS_RUN > Globals.HIGH_SCORE[Globals.DIFFICULTY])
            YouDiedNote = "Congrats! New high score: " + Globals.HIGH_SCORE_THIS_RUN.ToString();
        else if (Globals.HIGH_SCORE_THIS_RUN == Globals.HIGH_SCORE[Globals.DIFFICULTY])
            YouDiedNote = "Awwh, you almost had it! But not really...";
        else YouDiedNote = "What are you even doing?:/";

        YouDiedNotePos = new Vector2(
            Globals.VIRTUAL_WIDTH / 2 - UISettings.fontUI.MeasureString(YouDiedNote).X / 2,
            Globals.VIRTUAL_HEIGHT / 2 - UISettings.fontUI.MeasureString(YouDiedNote).Y * 2
        );

        ButtonRestart.SetOrder(0);
        ButtonMenu.SetOrder(1);

        ButtonRestart.UpdateHoverColor(audio);
        ButtonMenu.UpdateHoverColor(audio);

        if (ButtonRestart.Hover() || ButtonMenu.Hover()) {
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
        }

        return desiredCursor;
    }

    public void Draw()
    {
        Globals.SpriteBatch.DrawString(UISettings.fontBig, "YOU DIED", YouDiedPos, Globals.Red);
        Globals.SpriteBatch.DrawString(UISettings.fontUI, YouDiedNote, YouDiedNotePos, Color.White);

        ButtonRestart.Draw(UISettings.buttonFont);
        ButtonMenu.Draw(UISettings.buttonFont);
    }
}