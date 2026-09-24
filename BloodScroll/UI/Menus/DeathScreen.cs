using Microsoft.Xna.Framework;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

//
// THE DEATH SCREEN
//
// Two lines under the title. The first is the number - what this run was worth
// against the best you have ever managed - and the second is the game having an
// opinion about it. The number goes first because that is what is actually
// being asked, and it goes in the accent colour when it is a new best, which is
// the only time this screen is ever good news.
//

public class DeathScreen
{
    private const string TITLE = "YOU DIED";

    // BUTTONS
    private Button ButtonRestart, ButtonMenu;

    private string Note = "";
    private bool NewBest = false;

    public void LoadContent()
    {
        ButtonRestart = new Button("RESTART", UISettings.buttonSize);
        ButtonMenu = new Button("MENU", UISettings.buttonSize);
    }

    public MouseCursor Update(IAudioService audio)
    {
        BuildNote();

        MenuLayout layout = Measure();

        MouseCursor desiredCursor = layout.PlaceButtons(audio, UISettings.buttonSize,
            ButtonRestart, ButtonMenu);

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

    private void BuildNote()
    {
        int best = Globals.HIGH_SCORE[Globals.DIFFICULTY];
        int run = Globals.HIGH_SCORE_THIS_RUN;

        NewBest = run > best;

        string headline = NewBest
            ? "NEW HIGH SCORE:  " + run
            : "SCORE  " + run + "          BEST  " + best;

        string taunt;

        if (NewBest)
            taunt = "Congrats! High score!";
        else if (run == best)
            taunt = "Awwh, you almost had it...";
        else
            taunt = "Are you even trying?";

        Note = headline + "\n" + taunt;
    }

    private MenuLayout Measure()
        => new(TITLE, UISettings.fontBig, Note, UISettings.fontUI, 2, UISettings.buttonSize);

    public void Draw()
    {
        MenuLayout layout = Measure();

        layout.DrawPanel();

        // Lit from behind, the same treatment the title on the main menu gets -
        // these two are the only red words in the game
        layout.DrawTitle(UISettings.fontBig, TITLE, Globals.Red, lit: true);

        layout.DrawDivider();

        layout.DrawSubtitle(
            UISettings.fontUI,
            Note,
            NewBest ? UITheme.AccentBright : UITheme.TextPrimary,
            UITheme.TextMuted);

        MenuLayout.DrawButtons(UISettings.buttonFont, ButtonRestart, ButtonMenu);
    }
}
