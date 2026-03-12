using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

public class MainMenu
{
    // BUTTONS
    private Button ButtonPlay, ButtonSettings, ButtonExitGame;
    private Button ButtonDifficulty;

    // NAPISI IN DRUGI UI ELEMENTI
    private static Vector2 TitlePos, HighScoreNotePos;
    private string HighScoreNote = "CURRENT HIGH SCORE: 0";

    public void LoadContent(GraphicsDevice device)
    {
        //
        // POSITIONS FOR UI ELEMENTS
        //
        TitlePos = new Vector2(
            Globals.VIRTUAL_WIDTH / 2 - UISettings.titleFont.MeasureString("BLOOD SCROLL").X / 2,
            Globals.VIRTUAL_HEIGHT / 2 - UISettings.titleFont.MeasureString("BLOOD SCROLL").Y - UISettings.fontUI.MeasureString(HighScoreNote).Y * 2
        );
    
        //
        // BUTTONS
        //
        // Main Menu buttons
        ButtonPlay = new Button();
        ButtonPlay.LoadContent("PLAY", UISettings.buttonSize, device);

        ButtonDifficulty = new Button();
        ButtonDifficulty.LoadContent("EASY MODE", UISettings.buttonSize, device);

        ButtonSettings = new Button();
        ButtonSettings.LoadContent("SETTINGS", UISettings.buttonSize, device);

        ButtonExitGame = new Button();
        ButtonExitGame.LoadContent("EXIT", UISettings.buttonSize, device);
    }

    public MouseCursor Update(IAudioService audio)
    {
        MouseCursor desiredCursor;
        
        HighScoreNote = "HIGH SCORE (" + ButtonDifficulty.Text + "): " + Globals.HIGH_SCORE[Globals.DIFFICULTY].ToString();

        HighScoreNotePos = new Vector2(
            Globals.VIRTUAL_WIDTH / 2 - UISettings.fontUI.MeasureString(HighScoreNote).X / 2,
            Globals.VIRTUAL_HEIGHT / 2 - UISettings.fontUI.MeasureString(HighScoreNote).Y * 2
        );

        ButtonPlay.SetOrder(0);
        ButtonDifficulty.SetOrder(1);
        ButtonSettings.SetOrder(2);
        ButtonExitGame.SetOrder(3);

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

            if (Globals.DIFFICULTY == 0)
                ButtonDifficulty.ChangeText("BABY MODE");
            else if (Globals.DIFFICULTY == 1)
                ButtonDifficulty.ChangeText("MEDIUM");
            else ButtonDifficulty.ChangeText("HELL");
        }

        return desiredCursor;
    }

    public void Draw()
    {
        Globals.SpriteBatch.DrawString(UISettings.titleFont, "BLOOD SCROLL", TitlePos, Globals.Red);
        Globals.SpriteBatch.DrawString(UISettings.fontUI, HighScoreNote, HighScoreNotePos, Color.White);

        ButtonPlay.Draw(UISettings.buttonFont);
        ButtonDifficulty.Draw(UISettings.buttonFont);
        ButtonSettings.Draw(UISettings.buttonFont);
        ButtonExitGame.Draw(UISettings.buttonFont);
    }
}