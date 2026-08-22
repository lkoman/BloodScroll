using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    public void LoadContent(GraphicsDevice device)
    {
        ButtonRestart = new Button();
        ButtonRestart.LoadContent("RESTART", UISettings.buttonSize, device);

        ButtonMenu = new Button();
        ButtonMenu.LoadContent("MENU", UISettings.buttonSize, device);
    }

    public MouseCursor Update(IAudioService audio)
    {
        MouseCursor desiredCursor;

        BuildNote();

        MenuLayout layout = Measure();

        ButtonRestart.Place(layout.ButtonAt(0, UISettings.buttonSize));
        ButtonMenu.Place(layout.ButtonAt(1, UISettings.buttonSize));

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
            taunt = "Congrats! Now do it again.";
        else if (run == best)
            taunt = "Awwh, you almost had it! But not really...";
        else
            taunt = "What are you even doing?:/";

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

        ButtonRestart.Draw(UISettings.buttonFont);
        ButtonMenu.Draw(UISettings.buttonFont);
    }
}
