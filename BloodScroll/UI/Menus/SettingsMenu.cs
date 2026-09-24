using Microsoft.Xna.Framework;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

//
// SETTINGS
//
// Two toggles and a way back. Both toggles read their value straight off the
// thing they control every frame instead of remembering what they last set it
// to - that way a setting loaded from the save file, or changed anywhere else,
// still shows up correctly the first time this screen is opened.
//
// BOTH ARE WRITTEN DOWN THE MOMENT THEY CHANGE. A player who turns the sound
// off means it for more than one sitting, and being asked again on every
// launch is the game forgetting something it was told.
//

public class SettingsMenu
{
    private const string TITLE = "SETTINGS";

    // BUTTONS
    private Button ButtonFullScreen, SoundButton, ButtonMenu;

    public void LoadContent()
    {
        ButtonFullScreen = new Button("FULLSCREEN", UISettings.buttonSize);
        SoundButton = new Button("SOUND", UISettings.buttonSize);
        ButtonMenu = new Button("BACK", UISettings.buttonSize);
    }

    public MouseCursor Update(IAudioService audio)
    {
        ButtonFullScreen.SetValue(Globals.FULLSCREEN ? "ON" : "OFF");
        SoundButton.SetValue(audio.GetMasterVolume() == 0f ? "OFF" : "ON");

        MenuLayout layout = Measure();

        MouseCursor desiredCursor = layout.PlaceButtons(audio, UISettings.buttonSize,
            ButtonFullScreen, SoundButton, ButtonMenu);

        if (ButtonFullScreen.ButtonClicked(audio))
        {
            Globals.FULLSCREEN = !Globals.FULLSCREEN;

            Core.Graphics.IsFullScreen = Globals.FULLSCREEN;
            Core.Graphics.ApplyChanges();

            SaveManager.Save();
        }
        else if(SoundButton.ButtonClicked(audio))
        {
            Globals.MUTED = !Globals.MUTED;

            audio.SetMasterVolume(Globals.MUTED ? 0f : 1f);

            SaveManager.Save();
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

        MenuLayout.DrawButtons(UISettings.buttonFont,
            ButtonFullScreen, SoundButton, ButtonMenu);
    }
}
