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
    // 0 - baby, 1 - medium, 2 - hell. MEDIUM is what the game is tuned as and
    // the other two are it bent one step either way - see Difficulty, which is
    // the only place that reads this and the only place worth editing.
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

    // THE TWO SLOW GUNS. Each one wears the same colour in three places - the
    // gun in your hand, the shot leaving it and the cooldown bar in the corner -
    // so a bar filling up in the HUD is obviously the gun you are holding.
    public static Color BlastOrange { get; } = new Color(230, 106, 46); // the shell gun and its explosions
    public static Color StunPurple { get; } = new Color(154, 99, 214);  // the stun gun and its shot

    // A mob frozen by a stun shot. Nothing else in the game is drawn this cold,
    // so a pale blue mob means "this one is not moving" and nothing else.
    public static Color StunBlue { get; } = new Color(122, 190, 235);

    // THE GREEN BAT AND WHAT IT LEAVES IN YOU. Deliberately NOT HealGreen -
    // that one is the butterfly and it means "this helps". This is the sour,
    // yellowed version of it: the same family of colour gone wrong, worn by the
    // bat, by its shots and by the player while it is working on him.
    public static Color PoisonGreen { get; } = new Color(140, 176, 62);

    // THE ROOTING SPIDER AND ITS WEB. The brightest thing in the game, on
    // purpose. This used to be almost the colour of the background, on the
    // theory that a shot you cannot see coming is frightening - and it is, but
    // losing the controls to something the player never had a chance to read is
    // not difficulty, it is a coin flip. Fluorescent, so the spider is pickable
    // out of the ceiling and the web is dodgeable in the air.
    //
    // Louder than either green already in use (HealGreen, PoisonGreen) so it
    // cannot be read as the butterfly or the green bat.
    public static Color RootWeb { get; } = new Color(57, 255, 20);

    //
    // WHO FIRED IT
    //
    // No bullet in the game is a sprite any more - every one of them is the
    // same code drawn circle (see BulletTexture), so the COLOUR is the only
    // thing left that says where a shot came from and what it will do when it
    // lands. That makes these load bearing rather than decorative, and no two
    // of them may be close enough to be mistaken for each other mid fight.
    //
    // The mobs that already own a colour elsewhere keep it and are not listed
    // here: the green bat's shots are PoisonGreen, the shadow twin's are its
    // own SHADOW violet, and the two slow guns are BlastOrange and StunPurple.
    //
    public static Color PistolBlue { get; } = new Color(86, 168, 235);  // the starter pistol
    public static Color RifleGold { get; } = new Color(238, 216, 150);  // the long gun - deliberately PALE, see below
    public static Color BatPurple { get; } = new Color(176, 96, 236);   // the purple bat
    public static Color CrabRed { get; } = new Color(219, 58, 58);      // the crab, firing straight up
    public static Color BossYellow { get; } = new Color(255, 199, 26);  // the fire boss

    // The rifle and the fire boss are the one pair that could be confused -
    // both are warm and both come at you in numbers. So the rifle is washed out
    // almost to cream and the boss is the deepest, most saturated yellow in the
    // game: the bright one is always the one that is trying to kill you.

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