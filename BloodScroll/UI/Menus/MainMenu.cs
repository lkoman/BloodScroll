using Microsoft.Xna.Framework;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

//
// THE FRONT DOOR
//
// Title, a red rule under it, the high score you are trying to beat, and four
// buttons. The layout is not worked out here - MenuLayout measures the whole
// block and centres it, so this file only says what is on the screen and what
// each button does when it is pressed.
//
// DIFFICULTY IS A SETTING, NOT A COMMAND. It sits in the stack looking like the
// other three but it carries its current value on the right hand side, because
// a button whose label is "HELL" tells you what the game is set to but never
// tells you that clicking it will change it.
//
// THE SEED SITS UNDER IT, and is the same idea taken one step further: a
// setting with too many values to cycle through, so it is typed instead. It
// belongs in this stack rather than behind SETTINGS because it shapes the RUN,
// the way difficulty does - fullscreen and sound are about the machine.
//

public class MainMenu
{
    // BUTTONS
    private Button ButtonPlay, ButtonSettings, ButtonExitGame;
    private Button ButtonDifficulty;

    // Not a button. It is typed into - see SeedField.
    private SeedField seedField;

    // Which row each control owns. Named, because the seed field is placed
    // apart from the buttons and the two numberings have to agree.
    private const int ROW_PLAY = 0;
    private const int ROW_DIFFICULTY = 1;
    private const int ROW_SEED = 2;
    private const int ROW_SETTINGS = 3;
    private const int ROW_EXIT = 4;
    private const int ROW_COUNT = 5;

    private const string TITLE = "BLOOD SCROLL";

    private string HighScoreNote = "CURRENT HIGH SCORE: 0";

    private static readonly string[] DIFFICULTY_NAMES = ["BABY MODE", "MEDIUM", "HELL"];

    public void LoadContent()
    {
        ButtonPlay = new Button("PLAY", UISettings.buttonSize);

        ButtonDifficulty = new Button("DIFFICULTY", UISettings.buttonSize);
        ButtonDifficulty.SetValue(DIFFICULTY_NAMES[Globals.DIFFICULTY]);

        ButtonSettings = new Button("SETTINGS", UISettings.buttonSize);
        ButtonExitGame = new Button("EXIT", UISettings.buttonSize);

        seedField = new SeedField(UISettings.buttonSize);
    }

    public MouseCursor Update(IAudioService audio)
    {
        // Read off the button rather than kept twice - the difficulty the
        // button is showing IS the one whose score belongs up there
        HighScoreNote = "BEST ON " + ButtonDifficulty.Value + ":  " + Globals.HIGH_SCORE[Globals.DIFFICULTY];

        MenuLayout layout = Measure();

        // The seed field goes FIRST, because its Update is what notices a click
        // landing anywhere other than itself and puts the caret away - and a
        // click on PLAY is exactly such a click.
        seedField.Place(layout.ButtonAt(ROW_SEED, UISettings.buttonSize));
        MouseCursor seedCursor = seedField.Update(audio);

        MouseCursor desiredCursor = MouseCursor.Arrow;

        desiredCursor = Pick(desiredCursor, layout.PlaceButton(audio, UISettings.buttonSize, ROW_PLAY, ButtonPlay));
        desiredCursor = Pick(desiredCursor, layout.PlaceButton(audio, UISettings.buttonSize, ROW_DIFFICULTY, ButtonDifficulty));
        desiredCursor = Pick(desiredCursor, layout.PlaceButton(audio, UISettings.buttonSize, ROW_SETTINGS, ButtonSettings));
        desiredCursor = Pick(desiredCursor, layout.PlaceButton(audio, UISettings.buttonSize, ROW_EXIT, ButtonExitGame));
        desiredCursor = Pick(desiredCursor, seedCursor);

        if (ButtonPlay.ButtonClicked(audio))
        {
            Globals.START_GAME = true;
            desiredCursor = MouseCursor.Arrow;
        }
        else if (ButtonExitGame.ButtonClicked(audio))
        {
            Globals.EXIT_GAME = true;
            desiredCursor = MouseCursor.Arrow;
        }
        else if (ButtonSettings.ButtonClicked(audio))
        {
            // A caret left blinking on a screen nobody is looking at is a caret
            // that swallows the next key pressed anywhere
            seedField.Commit();

            Globals.SETTINGS_MENU = true;
        }
        else if(ButtonDifficulty.ButtonClicked(audio))
        {
            if (Globals.DIFFICULTY == 2)
                Globals.DIFFICULTY = 0;
            else Globals.DIFFICULTY++;

            ButtonDifficulty.SetValue(DIFFICULTY_NAMES[Globals.DIFFICULTY]);

            // Kept for the next launch, alongside the scores it belongs to
            SaveManager.Save();
        }

        return desiredCursor;
    }

    // A hand beats an arrow. Only one row can be under the mouse at a time, so
    // whichever of them wants the hand is the one that gets it.
    private static MouseCursor Pick(MouseCursor current, MouseCursor next)
        => next == MouseCursor.Hand ? next : current;

    // Measured in both Update and Draw off the same strings, so the buttons are
    // hit tested exactly where they were drawn
    private MenuLayout Measure()
        => new(TITLE, UISettings.titleFont,
               HighScoreNote, UISettings.fontUI,
               ROW_COUNT, UISettings.buttonSize);

    public void Draw()
    {
        MenuLayout layout = Measure();

        layout.DrawPanel();

        layout.DrawTitle(UISettings.titleFont, TITLE, Globals.Red, lit: true);

        layout.DrawDivider();

        layout.DrawSubtitle(UISettings.fontUI, HighScoreNote, UITheme.TextMuted);

        MenuLayout.DrawButtons(UISettings.buttonFont,
            ButtonPlay, ButtonDifficulty, ButtonSettings, ButtonExitGame);

        // Drawn where Update placed it. Update runs first every frame, so the
        // slab is hit tested exactly where it is about to be painted.
        seedField.Draw(UISettings.buttonFont);
    }
}
