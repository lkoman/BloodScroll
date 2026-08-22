using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;
using System;

namespace BloodScroll;

//
// SETTINGS
//
// Two toggles and a way back. Both toggles read their value straight off the
// thing they control every frame instead of remembering what they last set it
// to - that way a setting loaded from the save file, or changed anywhere else,
// still shows up correctly the first time this screen is opened.
//

public class SettingsMenu
{
    private const string TITLE = "SETTINGS";

    // BUTTONS
    private Button ButtonFullScreen, SoundButton, ButtonMenu;

    public void LoadContent(GraphicsDevice device)
    {
        ButtonFullScreen = new Button();
        ButtonFullScreen.LoadContent("FULLSCREEN", UISettings.buttonSize, device);

        SoundButton = new Button();
        SoundButton.LoadContent("SOUND", UISettings.buttonSize, device);

        ButtonMenu = new Button();
        ButtonMenu.LoadContent("BACK", UISettings.buttonSize, device);
    }

    public MouseCursor Update(IAudioService audio)
    {
        MouseCursor desiredCursor;

        ButtonFullScreen.SetValue(Globals.FULLSCREEN ? "ON" : "OFF");
        SoundButton.SetValue(audio.GetMasterVolume() == 0f ? "OFF" : "ON");

        MenuLayout layout = Measure();

        ButtonFullScreen.Place(layout.ButtonAt(0, UISettings.buttonSize));
        SoundButton.Place(layout.ButtonAt(1, UISettings.buttonSize));
        ButtonMenu.Place(layout.ButtonAt(2, UISettings.buttonSize));

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
            Globals.FULLSCREEN = !Globals.FULLSCREEN;

            Core.Graphics.IsFullScreen = Globals.FULLSCREEN;
            Core.Graphics.ApplyChanges();
        }
        else if(SoundButton.ButtonClicked(audio))
        {
            audio.SetMasterVolume(audio.GetMasterVolume() == 0f ? 1f : 0f);
        }
        else if(ButtonMenu.ButtonClicked(audio))
        {
            Globals.MENU = true;
            Globals.SETTINGS_MENU = false;
        }

        return desiredCursor;
    }

    private static MenuLayout Measure()
        => new(TITLE, UISettings.fontBig, null, UISettings.fontUI, 3, UISettings.buttonSize);

    public void Draw()
    {
        MenuLayout layout = Measure();

        layout.DrawPanel();

        layout.DrawTitle(UISettings.fontBig, TITLE, UITheme.TextPrimary);

        layout.DrawDivider();

        ButtonFullScreen.Draw(UISettings.buttonFont);
        SoundButton.Draw(UISettings.buttonFont);
        ButtonMenu.Draw(UISettings.buttonFont);
    }
}
