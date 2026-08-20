using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;
using System;

namespace BloodScroll;

public class SettingsMenu
{
    private static Vector2 SettingsPos;

    private string OneLine = "-";

    // BUTTONS
    private Button ButtonFullScreen, SoundButton, ButtonMenu;

    public void LoadContent(GraphicsDevice device)
    {
        //
        // POSITIONS FOR UI ELEMENTS
        //
        SettingsPos = new Vector2(
            Globals.VIRTUAL_WIDTH / 2 - UISettings.fontBig.MeasureString("SETTINGS").X / 2,
            Globals.VIRTUAL_HEIGHT / 2 - UISettings.fontBig.MeasureString("SETTINGS").Y - UISettings.fontUI.MeasureString(OneLine).Y * 2
        );

        // BUTTONS
        ButtonFullScreen = new Button();
        ButtonFullScreen.LoadContent("FULLSCREEN", UISettings.buttonSize, device);
    
        SoundButton = new Button();
        SoundButton.LoadContent("SOUND ON", UISettings.buttonSize, device);

        ButtonMenu = new Button();
        ButtonMenu.LoadContent("MENU", UISettings.buttonSize, device);

    }

    public MouseCursor Update(IAudioService audio)
    {
        MouseCursor desiredCursor;

        ButtonFullScreen.SetOrder(0);
        SoundButton.SetOrder(1);
        ButtonMenu.SetOrder(2);

        ButtonFullScreen.UpdateHoverColor(audio);
        SoundButton.UpdateHoverColor(audio);
        ButtonMenu.UpdateHoverColor(audio);

        if (ButtonFullScreen.Hover() || SoundButton.Hover() || ButtonMenu.Hover()) {
            desiredCursor = MouseCursor.Hand;
        }
        else {
            desiredCursor = MouseCursor.Arrow;
        }
        
        if (ButtonFullScreen.ButtonClicked(audio))
        {
            if (Globals.FULLSCREEN == false)
            {
                Globals.FULLSCREEN = true;
                ButtonFullScreen.ChangeText("WINDOWED");

                Core.Graphics.IsFullScreen = true;
                Core.Graphics.ApplyChanges();
            }
            else
            {
                Globals.FULLSCREEN = false;
                ButtonFullScreen.ChangeText("FULLSCREEN");

                Core.Graphics.IsFullScreen = false;
                Core.Graphics.ApplyChanges();
            }
        }
        else if(SoundButton.ButtonClicked(audio))
        {
            if (audio.GetMasterVolume() == 0f)
            {
                audio.SetMasterVolume(1f);
                SoundButton.ChangeText("SOUND ON");
            }
            else {
                audio.SetMasterVolume(0f);
                SoundButton.ChangeText("SOUND OFF");
            }
        }
        else if(ButtonMenu.ButtonClicked(audio))
        {
            Globals.MENU = true;
            Globals.SETTINGS_MENU = false;
        }

        return desiredCursor;
    }

    public void Draw()
    {
        Globals.SpriteBatch.DrawString(UISettings.fontBig, "SETTINGS", SettingsPos, Globals.AlmostWhite);

        ButtonFullScreen.Draw(UISettings.buttonFont);
        SoundButton.Draw(UISettings.buttonFont);
        ButtonMenu.Draw(UISettings.buttonFont);
    }
}