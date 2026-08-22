using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

//
// THE PAUSE SCREEN
//
// The same panel as the main menu, over the frozen game rather than over the
// menu background. The score you are currently sitting on goes under the title:
// pausing is the one moment you can actually read it, and it is the number that
// decides whether you carry on or restart.
//

public class PauseScreen
{
    private const string TITLE = "PAUSED";

    // BUTTONS
    private Button ButtonRestart, ButtonResume, ButtonMenu;

    private string ScoreNote = "";

    public void LoadContent(GraphicsDevice device)
    {
        ButtonResume = new Button();
        ButtonResume.LoadContent("RESUME", UISettings.buttonSize, device);

        ButtonRestart = new Button();
        ButtonRestart.LoadContent("RESTART", UISettings.buttonSize, device);

        ButtonMenu = new Button();
        ButtonMenu.LoadContent("MENU", UISettings.buttonSize, device);
    }

    public MouseCursor Update(IAudioService audio)
    {
        MouseCursor desiredCursor;

        ScoreNote = "SCORE " + Globals.POINTS + "          BEST " + Globals.HIGH_SCORE[Globals.DIFFICULTY];

        MenuLayout layout = Measure();

        ButtonResume.Place(layout.ButtonAt(0, UISettings.buttonSize));
        ButtonRestart.Place(layout.ButtonAt(1, UISettings.buttonSize));
        ButtonMenu.Place(layout.ButtonAt(2, UISettings.buttonSize));

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

    private MenuLayout Measure()
        => new(TITLE, UISettings.fontBig, ScoreNote, UISettings.fontUI, 3, UISettings.buttonSize);

    public void Draw()
    {
        MenuLayout layout = Measure();

        layout.DrawPanel();

        layout.DrawTitle(UISettings.fontBig, TITLE, UITheme.TextPrimary);

        layout.DrawDivider();

        layout.DrawSubtitle(UISettings.fontUI, ScoreNote, UITheme.TextMuted);

        ButtonResume.Draw(UISettings.buttonFont);
        ButtonRestart.Draw(UISettings.buttonFont);
        ButtonMenu.Draw(UISettings.buttonFont);
    }
}
