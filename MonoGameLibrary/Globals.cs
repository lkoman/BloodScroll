using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using Microsoft.Xna.Framework.Input;

namespace MonoGameLibrary;

public static class Globals
{    
    // GAME STATES
    public static bool EXIT_GAME { get; set; } = false;
    public static bool MENU { get; set; } = true;
    public static bool START_GAME { get; set; } = false; // load gameplay
    public static bool PAUSE { get; set; } = false;
    public static bool DISPLAY_PAUSE_MENU { get; set; } = false;
    public static bool RESTART { get; set; } = false;
    public static bool SETTINGS_MENU { get; set; } = false;

    // GAMEPLAY
    // 0 baby, 1 medium, 2 hell. See Difficulty - the only place that reads this
    // and the only place worth editing.
    public static int DIFFICULTY { get; set; } = 1;
    public static bool PLAYER_ALIVE { get; set; } = true;
    public static int[] HIGH_SCORE { get; set; } = [0, 0, 0];
    public static int HIGH_SCORE_THIS_RUN { get; set; } = 0;
    public static int POINTS { get; set; } = 0;
    public static int CurrentLayerIndex { get; set; } = 0; // Layer index where the player is standing
    public static string CurrentLayerType { get; set; } = "Ground Level";

    // Game variables - screen
    public static bool FULLSCREEN { get; set; } = true;
    public const int VIRTUAL_WIDTH = 1920;//3840;
    public const int VIRTUAL_HEIGHT = 1080;//2160;
    public static Vector2 CameraOffset { get; set; } = Vector2.Zero;
    public static float GroundHeight; // Height at which all non flying characters will be standing

    // Game variables - time and seed
    public static float DT { get; set; }
    public static TimeSpan ElapsedTime { get; set; }
    public static Random R { get; set; }
    public static int SEED { get; set; }

    // KEYBOARD INPUT
    public static KeyboardState LastKeyboardState { get; set; }
    public static KeyboardState CurrentKeyboardState { get; set; }

    // MOUSE INPUT
    public static MouseState MouseState { get; set; }
    public static MouseState LastMouseState { get; set; }
    public static Vector2 MousePosition { get; set; }
    public static Rectangle Cursor { get; set; }

    // Spritebatch and ATLASES
    public static SpriteBatch SpriteBatch { get; set; }
    public static TextureAtlas UI { get; set; }
    public static TextureAtlas Enemies { get; set; }
    public static TextureAtlas Crab { get; set; }
    public static TextureAtlas Jellyfish { get; set; }
    public static TextureAtlas Butterfly { get; set; }
    public static TextureAtlas Spiders { get; set; }
    public static TextureAtlas Flower { get; set; }
    public static TextureAtlas MothBoss { get; set; }
    public static TextureAtlas Player { get; set; }
    public static TextureAtlas Weapons { get; set; }
    public static TextureAtlas World { get; set; }
    public static TextureAtlas Backgrounds { get; set; }
    public static TextureAtlas Foregrounds { get; set; }

    // GLOBAL COLORS
    public static Color ScreenOverlayColor { get; set; } = Color.Transparent;
    public static readonly Color BackgroundOverlayColor = Color.White * 0.5f;
    public static Color DarkRed { get; } = new Color(43, 12, 12); //(52, 28, 39);
    public static Color Red { get; } = new Color(165, 48, 48);
    public static Color Gray { get; } = new Color(87, 114, 119);
    public static Color DarkGray { get; } = new Color(57, 74, 80);
    public static Color AlmostBlack { get; } = new Color(9, 10, 20);
    public static Color AlmostWhite { get; } = new Color(235, 237, 233);
    public static Color HotPink { get; } = new Color(198, 81, 151);
    public static Color Yellow { get; } = new Color(255, 224, 92); // the shield
    public static Color HealGreen { get; } = new Color(99, 199, 77); // the butterfly - the only thing on screen that helps

    // THE TWO SLOW GUNS. Each wears the same colour in three places: the gun,
    // its shot, and its cooldown bar in the HUD.
    public static Color BlastOrange { get; } = new Color(230, 106, 46); // the shell gun and its explosions
    public static Color StunPurple { get; } = new Color(154, 99, 214);  // the stun gun and its shot

    // THE SWORD - the only bar in the HUD that does not belong to a gun. Cold,
    // so it is not confused with the rifle's cream or the shell's orange.
    public static Color SwordSteel { get; } = new Color(198, 214, 226);

    // A mob frozen by a stun shot. Nothing else is drawn this cold.
    public static Color StunBlue { get; } = new Color(122, 190, 235);

    // THE GREEN BAT AND ITS POISON. NOT HealGreen - that is the butterfly and
    // means "this helps". Worn by the bat, its shots, and the poisoned player.
    public static Color PoisonGreen { get; } = new Color(140, 176, 62);

    // THE ROOTING SPIDER AND ITS WEB. The brightest thing in the game, so the
    // spider can be picked out of the ceiling and the web dodged in the air.
    // Louder than HealGreen and PoisonGreen so it cannot be confused with them.
    public static Color RootWeb { get; } = new Color(57, 255, 20);

    //
    // WHO FIRED IT
    //
    // Every bullet is the same code drawn circle (see BulletTexture), so COLOUR
    // is the only thing saying where a shot came from. LOAD BEARING, not
    // decorative - no two may be close enough to confuse mid fight.
    //
    // Mobs that own a colour elsewhere are not listed here: the green bat's
    // shots are PoisonGreen, the shadow twin's its own SHADOW violet, and the
    // two slow guns BlastOrange and StunPurple.
    //
    public static Color PistolBlue { get; } = new Color(86, 168, 235);  // the starter pistol
    public static Color RifleGold { get; } = new Color(238, 216, 150);  // the long gun - deliberately PALE, see below
    public static Color BatPurple { get; } = new Color(176, 96, 236);   // the purple bat
    public static Color CrabRed { get; } = new Color(219, 58, 58);      // the crab, firing straight up
    public static Color BossYellow { get; } = new Color(255, 199, 26);  // the fire boss

    // The rifle and the fire boss are the pair most easily confused - both warm.
    // The rifle is washed almost to cream, the boss is the most saturated yellow
    // in the game: THE BRIGHT ONE IS THE ONE TRYING TO KILL YOU.

    public static void Update(GameTime gameTime)
    {
        // Fixed DT
        DT = 1f / 60f; // simulate 60 FPS fixed timestep
        ElapsedTime = TimeSpan.FromSeconds(DT);

        //DT = (float)gameTime.ElapsedGameTime.TotalSeconds;
        //ElapsedTime = gameTime.ElapsedGameTime;

        LastMouseState = MouseState;
        MouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();

        MousePosition = new Vector2 (MouseState.X, MouseState.Y) - CameraOffset; // world position
        Cursor = new Rectangle((int)MousePosition.X, (int)MousePosition.Y, 1, 1);
    }
}