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
    public static bool FIRST_GAME { get; set; } = true;
    public static bool PAUSE { get; set; } = false;
    public static bool DISPLAY_PAUSE_MENU { get; set; } = false;
    public static bool RESTART { get; set; } = false;
    public static bool SETTINGS_MENU { get; set; } = false;

    // GAMEPLAY
    public static int DIFFICULTY { get; set; } = 0; // 0 - easy, 1 - medium, 2 - hard
    public static bool PLAYER_ALIVE { get; set; } = true;
    public static int[] HIGH_SCORE { get; set; } = [0, 0, 0];
    public static int HIGH_SCORE_THIS_RUN { get; set; } = 0;
    public static int POINTS { get; set; } = 0;
    public static int CurrentLayerIndex { get; set; } = 0; // Layer index where the player is standing
    public static int NextLayerIndex { get; set; } = 0;
    public static string CurrentLayerType { get; set; } = "Ground Level";

    // Game variables - screen
    public static bool FULLSCREEN { get; set; } = false;
    public const int VIRTUAL_WIDTH = 1920;//3840;
    public const int VIRTUAL_HEIGHT = 1080;//2160;
    public static Vector2 CameraOffset { get; set; } = Vector2.Zero;

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
    public static bool HoldingLeftButton { get; set; } = false;
    public static Rectangle Cursor { get; set; }

    // Spritebatch and ATLASES
    public static SpriteBatch SpriteBatch { get; set; }
    public static TextureAtlas UI { get; set; }
    public static TextureAtlas Enemies { get; set; }
    public static TextureAtlas Player { get; set; }
    public static TextureAtlas Weapons { get; set; }
    public static TextureAtlas World { get; set; }
    public static TextureAtlas Backgrounds { get; set; }

    // GLOBAL COLORS
    public static Color ScreenOverlayColor { get; set; } = Color.Transparent;
    public static readonly Color BackgroundOverlayColor = Color.White * 0.5f;
    public static Color DarkRed { get; } = new Color(43, 12, 12); //(52, 28, 39);
    public static Color Red { get; } = new Color(165, 48, 48);
    public static Color Gray { get; } = new Color(87, 114, 119);
    public static Color DarkGray { get; } = new Color(57, 74, 80);
    public static Color AlmostBlack { get; } = new Color(9, 10, 20);
    public static Color AlmostWhite { get; } = new Color(235, 237, 233);

    // CUSTOM EFFECTS
    public static Effect whiteFlashEffect;

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