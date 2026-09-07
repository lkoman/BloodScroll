using Microsoft.Xna.Framework;
using MonoGameLibrary;
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

    public void LoadContent()
    {
        ButtonResume = new Button("RESUME", UISettings.buttonSize);
        ButtonRestart = new Button("RESTART", UISettings.buttonSize);
        ButtonMenu = new Button("MENU", UISettings.buttonSize);
    }

    public MouseCursor Update(IAudioService audio)
    {
        ScoreNote = "SCORE " + Globals.POINTS + "          BEST " + Globals.HIGH_SCORE[Globals.DIFFICULTY];

        MenuLayout layout = Measure();

        MouseCursor desiredCursor = layout.PlaceButtons(audio, UISettings.buttonSize,
            ButtonResume, ButtonRestart, ButtonMenu);

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

        MenuLayout.DrawButtons(UISettings.buttonFont,
            ButtonResume, ButtonRestart, ButtonMenu);
    }
}
