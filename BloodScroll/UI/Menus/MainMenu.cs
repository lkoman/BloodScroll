using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

public class MainMenu
{
    // BUTTONS
    private Button ButtonPlay, ButtonSettings, ButtonExitGame;
    private Button ButtonDifficulty;

    private const string TITLE = "BLOOD SCROLL";

    private string HighScoreNote = "CURRENT HIGH SCORE: 0";

    // Along the bottom of the panel, under the buttons - the one place the
    // controls are written down
    private const string HINT = "A / D  MOVE          SPACE  JUMP          E  PICK UP          ESC  PAUSE";
    private const float HINT_SCALE = 0.62f;
    private const int HINT_DROP = 26;

    private static readonly string[] DIFFICULTY_NAMES = ["BABY MODE", "MEDIUM", "HELL"];

    public void LoadContent(GraphicsDevice device)
    {
        ButtonPlay = new Button();
        ButtonPlay.LoadContent("PLAY", UISettings.buttonSize, device);

        ButtonDifficulty = new Button();
        ButtonDifficulty.LoadContent("DIFFICULTY", UISettings.buttonSize, device);
        ButtonDifficulty.SetValue(DIFFICULTY_NAMES[Globals.DIFFICULTY]);

        ButtonSettings = new Button();
        ButtonSettings.LoadContent("SETTINGS", UISettings.buttonSize, device);

        ButtonExitGame = new Button();
        ButtonExitGame.LoadContent("EXIT", UISettings.buttonSize, device);
    }

    public MouseCursor Update(IAudioService audio)
    {
        MouseCursor desiredCursor;

        // Read off the button rather than kept twice - the difficulty the
        // button is showing IS the one whose score belongs up there
        HighScoreNote = "BEST ON " + ButtonDifficulty.Value + ":  " + Globals.HIGH_SCORE[Globals.DIFFICULTY];

        MenuLayout layout = Measure();

        ButtonPlay.Place(layout.ButtonAt(0, UISettings.buttonSize));
        ButtonDifficulty.Place(layout.ButtonAt(1, UISettings.buttonSize));
        ButtonSettings.Place(layout.ButtonAt(2, UISettings.buttonSize));
        ButtonExitGame.Place(layout.ButtonAt(3, UISettings.buttonSize));

        ButtonPlay.UpdateHoverColor(audio);
        ButtonSettings.UpdateHoverColor(audio);
        ButtonExitGame.UpdateHoverColor(audio);
        ButtonDifficulty.UpdateHoverColor(audio);

        if (ButtonPlay.Hover() || ButtonSettings.Hover() || ButtonExitGame.Hover() || ButtonDifficulty.Hover()) {
            desiredCursor = MouseCursor.Hand;
        }
        else {
            desiredCursor = MouseCursor.Arrow;
        }

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
            Globals.SETTINGS_MENU = true;
        }
        else if(ButtonDifficulty.ButtonClicked(audio))
        {
            if (Globals.DIFFICULTY == 2)
                Globals.DIFFICULTY = 0;
            else Globals.DIFFICULTY++;

            ButtonDifficulty.SetValue(DIFFICULTY_NAMES[Globals.DIFFICULTY]);
        }

        return desiredCursor;
    }

    // Measured in both Update and Draw off the same strings, so the buttons are
    // hit tested exactly where they were drawn
    private MenuLayout Measure()
        => new(TITLE, UISettings.titleFont,
               HighScoreNote, UISettings.fontUI,
               4, UISettings.buttonSize);

    public void Draw()
    {
        MenuLayout layout = Measure();

        layout.DrawPanel();

        layout.DrawTitle(UISettings.titleFont, TITLE, Globals.Red, lit: true);

        layout.DrawDivider();

        layout.DrawSubtitle(UISettings.fontUI, HighScoreNote, UITheme.TextMuted);

        ButtonPlay.Draw(UISettings.buttonFont);
        ButtonDifficulty.Draw(UISettings.buttonFont);
        ButtonSettings.Draw(UISettings.buttonFont);
        ButtonExitGame.Draw(UISettings.buttonFont);

        DrawHint(layout);
    }

    // Small, muted and just below the panel rather than inside it: it is a
    // reminder, and it must never compete with the buttons for attention
    private static void DrawHint(MenuLayout layout)
    {
        UITheme.DrawTextCentred(
            UISettings.fontUI,
            HINT,
            Globals.VIRTUAL_WIDTH / 2f,
            layout.Panel.Bottom + HINT_DROP,
            UITheme.TextMuted * 0.75f,
            HINT_SCALE);
    }
}
