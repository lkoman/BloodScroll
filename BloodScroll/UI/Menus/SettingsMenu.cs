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
// CONTROLS is not a toggle but a page of its own. It is held here rather than
// given a flag in Globals, because it only ever opens from this screen and
// only ever closes back into it.
//

public class SettingsMenu
{
    private const string TITLE = "SETTINGS";

    // BUTTONS
    private Button ButtonFullScreen, SoundButton, ButtonControls, ButtonMenu;

    private ControlsMenu controlsMenu;
    private bool showingControls;

    public void LoadContent()
    {
        ButtonFullScreen = new Button("FULLSCREEN", UISettings.buttonSize);
        SoundButton = new Button("SOUND", UISettings.buttonSize);
        ButtonControls = new Button("CONTROLS", UISettings.buttonSize);
        ButtonMenu = new Button("BACK", UISettings.buttonSize);

        controlsMenu = new();
        controlsMenu.LoadContent();
    }

    public MouseCursor Update(IAudioService audio)
    {
        if (showingControls)
        {
            if (controlsMenu.Update(audio, out MouseCursor controlsCursor))
                showingControls = false;

            return controlsCursor;
        }

        ButtonFullScreen.SetValue(Globals.FULLSCREEN ? "ON" : "OFF");
        SoundButton.SetValue(audio.GetMasterVolume() == 0f ? "OFF" : "ON");

        MenuLayout layout = Measure();

        MouseCursor desiredCursor = layout.PlaceButtons(audio, UISettings.buttonSize,
            ButtonFullScreen, SoundButton, ButtonControls, ButtonMenu);

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
        else if(ButtonControls.ButtonClicked(audio))
        {
            showingControls = true;
        }
        else if(ButtonMenu.ButtonClicked(audio))
        {
            Globals.MENU = true;
            Globals.SETTINGS_MENU = false;
        }

        return desiredCursor;
    }

    private static MenuLayout Measure()
        => new(TITLE, UISettings.fontBig, null, UISettings.fontUI, 4, UISettings.buttonSize);

    public void Draw()
    {
        if (showingControls)
        {
            controlsMenu.Draw();
            return;
        }

        MenuLayout layout = Measure();

        layout.DrawPanel();

        layout.DrawTitle(UISettings.fontBig, TITLE, UITheme.TextPrimary);

        layout.DrawDivider();

        MenuLayout.DrawButtons(UISettings.buttonFont,
            ButtonFullScreen, SoundButton, ButtonControls, ButtonMenu);
    }
}
