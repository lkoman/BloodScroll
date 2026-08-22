using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Debug;
using System.Collections.Generic;

namespace BloodScroll;

public class BloodScroll : Core
{
    // CLASSES 
    public AudioService audioService;
    public WeaponsManager weaponsManager;
    private CollisionResponse collisionResponse;
    public UI userInterface;
    private GameWorld gameWorld;
    private Player player;

    // OTHER VARIABLES
    // Purely about one time content loading - the sprites and atlases only need
    // building once, however many runs the player starts.
    private bool contentLoaded = false;
    private List<IDrawableLayer> drawables = [];
    private Matrix camMatrix;
    private Sprite _foreground;

    // How solid the framing art at the edges is. Short of 1, so what passes
    // behind it can still be made out - see LoadContent.
    private const float FOREGROUND_ALPHA = 0.75f;

    // DEBUG
    // The function keys and what they switch on live in DebugMode - this is
    // only the thing that draws the boxes F1 asks for.
    public DebugRenderer debugRenderer;

    // Scroll switch screen
    private bool isTransitioning = false;
    private Vector2 startCameraOffset;
    private Vector2 targetCameraOffset;
    private float transitionDuration = 0.1f; // seconds
    private float transitionTimer = 0f;
    private int prevLayerIndex = 0;
    private int currentLayerIndex = 0;

    public BloodScroll() : base ("BloodScroll", Globals.VIRTUAL_WIDTH, Globals.VIRTUAL_HEIGHT, Globals.FULLSCREEN) {}

    protected override void Initialize()
    {
        debugRenderer = new DebugRenderer();
        userInterface = new UI();

        base.Initialize();

        Globals.SpriteBatch = new SpriteBatch(GraphicsDevice);
        debugRenderer.LoadContent(GraphicsDevice);

        LoadGameClasses();
    }

    public void LoadGameClasses()
    {
        // get saved settings
        SaveManager.Load();

        audioService = new AudioService(Content);
        userInterface.LoadContent(GraphicsDevice, audioService);

        gameWorld = new GameWorld();
        player = new Player();
        weaponsManager = new WeaponsManager();
        collisionResponse = new CollisionResponse();

        // START MENU MUSIC
        audioService.PlayMusic(AudioId.MenuMusic);
    }

    public void StartGame()
    {
        if (!contentLoaded)
        {
            player.LoadContent();
            weaponsManager.LoadContent(player);
            gameWorld.LoadContent(audioService);

            contentLoaded = true;
        }

        Restart();
    }

    private void Restart()
    {
        UpdateHighScore();
        Globals.R = new Random(Globals.SEED);

        Globals.RESTART = false;
        Globals.PLAYER_ALIVE = true;
        Globals.PAUSE = false;
        Globals.CameraOffset = Vector2.Zero;
        Globals.POINTS = 0;
        Globals.CurrentLayerIndex = 0;
        Globals.SETTINGS_MENU = false;

        player.Restart();
        weaponsManager.Restart(player);
        gameWorld.Restart();

        userInterface.GiftCardDisplayed = false;

        // LAST, after everything above has been wiped back to a fresh run. If
        // debug mode is on it hands the guns and the shield straight back, so
        // the switch keeps meaning the same thing from one run to the next.
        DebugMode.ApplyToRun(player, weaponsManager);

        // Restart music
        audioService.SwitchToGameMusic();
    }

    protected override void LoadContent()
    {
        Globals.UI = TextureAtlas.FromFile(Content, "images/UI.xml");
        Globals.Backgrounds = TextureAtlas.FromFile(Content, "images/backgrounds.xml");
        Globals.Foregrounds = TextureAtlas.FromFile(Content, "images/foreground.xml");
        Globals.Enemies = TextureAtlas.FromFile(Content, "images/enemies.xml");
        Globals.Crab = TextureAtlas.FromFile(Content, "images/crab.xml");
        Globals.Jellyfish = TextureAtlas.FromFile(Content, "images/jellyfish.xml");
        Globals.Butterfly = TextureAtlas.FromFile(Content, "images/butterfly.xml");
        Globals.Spiders = TextureAtlas.FromFile(Content, "images/spiders.xml");
        Globals.Flower = TextureAtlas.FromFile(Content, "images/flower.xml");
        Globals.MothBoss = TextureAtlas.FromFile(Content, "images/MothBoss.xml");
        Globals.Player = TextureAtlas.FromFile(Content, "images/player.xml");
        Globals.World = TextureAtlas.FromFile(Content, "images/world.xml");
        Globals.Weapons = TextureAtlas.FromFile(Content, "images/weapons.xml");

        Globals.SEED = 420;
        Globals.R = new Random(Globals.SEED);

        // Height at which all non flying characters will be standing
        Globals.GroundHeight = windowHeight - 250;

        // FOREGROUND SPRITE (aesthetics)
        //
        // It is drawn over EVERYTHING - the mobs, the player, his bullets - so
        // anything that wanders under the frame at the edges of the screen is
        // simply gone while it is there. Held back a little, so the edges read
        // as something in front of the room rather than a hole in it: a bat
        // coming in from the side is a shape behind the art instead of a bat
        // that was not on screen a moment ago.
        _foreground = Globals.Foregrounds.CreateSprite("foreground");
        _foreground.Color = Color.White * FOREGROUND_ALPHA;

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        if (!IsActive)
            return;

        CheckIfGamePaused();
        CheckDebugKeys();
        UpdateScreenOverlay();

        if (Globals.PAUSE || !Globals.PLAYER_ALIVE)
        {
            if (Globals.POINTS > Globals.HIGH_SCORE_THIS_RUN)
                Globals.HIGH_SCORE_THIS_RUN = Globals.POINTS;
        }

        userInterface.Update(audioService);
        Globals.Update(gameTime);
        
        if (Globals.MENU)
        {
            UpdateHighScore();

            if (Globals.EXIT_GAME)
                Exit();

            if (!Globals.START_GAME)
                return;
            
            // START GAME (button play was clicked). StartGame restarts the run
            // itself, music included, so nothing more is needed here - asking
            // for a RESTART on top of it built the whole world a second time.
            StartGame();

            Globals.MENU = false;
            Globals.START_GAME = false;
        }

        if (Globals.RESTART)
            Restart();
        
        if (Globals.PAUSE)
            return;
        
        GetLayerIndex();
        CheckCameraOffset();
        LayerTransition();
        
        player.Update(audioService, weaponsManager);
        weaponsManager.Update(player, audioService, gameWorld);

        collisionResponse.HandleAllCollisions(player, gameWorld, weaponsManager, audioService);
        gameWorld.UpdateLevel(player, weaponsManager, userInterface);

        base.Update(gameTime);
    }

    private void GetLayerIndex()
    {
        Globals.CurrentLayerIndex = Math.Abs((int)Math.Floor(player.Bottom / windowHeight)); // Get current layer index
        Globals.CurrentLayerType = gameWorld.GetCurrentLayerType();
    }

    private void CheckCameraOffset()
    {
        if (isTransitioning)
            return;
        
        prevLayerIndex = currentLayerIndex;
        currentLayerIndex = Globals.CurrentLayerIndex;

        if (prevLayerIndex != currentLayerIndex)
        {
            startCameraOffset = Globals.CameraOffset;
            targetCameraOffset = new Vector2(0, windowHeight * currentLayerIndex);

            transitionTimer = 0f;
            isTransitioning = true;
        }
    }

    private void LayerTransition()
    {
        if (isTransitioning)
        {
            transitionTimer += Globals.DT;
            float t = MathHelper.Clamp(transitionTimer / transitionDuration, 0f, 1f);
            Globals.CameraOffset = Vector2.Lerp(startCameraOffset, targetCameraOffset, t);

            if (t >= 1f)
                isTransitioning = false;
            
            return;
        }
        Globals.CameraOffset = new Vector2(0f, windowHeight * Globals.CurrentLayerIndex);
    }

    private static void UpdateHighScore()
    {
        if (Globals.HIGH_SCORE_THIS_RUN > Globals.HIGH_SCORE[Globals.DIFFICULTY])
        {
            Globals.HIGH_SCORE[Globals.DIFFICULTY] = Globals.HIGH_SCORE_THIS_RUN;
            SaveManager.Save();
        }

        Globals.HIGH_SCORE_THIS_RUN = 0;
        Globals.POINTS = 0;
    }

    private static void UpdateScreenOverlay()
    {
        if (Globals.PAUSE || Globals.MENU)
            Globals.ScreenOverlayColor = Color.Black * 0.6f;

        else if (!Globals.PLAYER_ALIVE)
            Globals.ScreenOverlayColor = Globals.DarkRed * 0.8f;
        
        else
            Globals.ScreenOverlayColor = Color.Transparent;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        drawables.Clear();

        if (Globals.MENU)
        {
            userInterface.Draw(player);
        }

        // GAMEPLAY
        else {
            drawables.AddRange(weaponsManager.GetDrawables()); // Gun + player bullets
            drawables.AddRange(gameWorld.GetDrawables()); // Monster bullets + current layer backgrond + current layer platforms + all enemies from all layers
            drawables.Add(player); // Player

            drawables.Sort((a, b) => a.DrawLayer.CompareTo(b.DrawLayer));

            DrawSortedLayers();

            userInterface.Draw(player, weaponsManager);

            // Last, so the hitboxes stay readable over the HUD and over the
            // dark overlay that covers the screen on death or pause
            if (DebugMode.ShowHitboxes)
                DrawDebugBoundingBoxes();
        }

        base.Draw(gameTime);
    }

    private void DrawSortedLayers()
    {
        camMatrix = Matrix.CreateTranslation(new Vector3(Globals.CameraOffset, 0f));

        Globals.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: camMatrix
        );

        foreach (var d in drawables)
            d.Draw();

        Globals.SpriteBatch.End();

        // Draw foreground above all
        DrawForeGround();
    }

    private void DrawForeGround()
    {
        Globals.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _foreground.Draw();
        Globals.SpriteBatch.End();
    }

    private void CheckIfGamePaused()
    {
        if (!Globals.PLAYER_ALIVE || Globals.MENU)
            return;
        
        Globals.CurrentKeyboardState = Keyboard.GetState();

        // Pause if gift card is displayed
        if (userInterface.GiftCardDisplayed)
            Globals.PAUSE = true;

        // Toggle pause on Escape press (only when pressed this frame)
        else if (Globals.CurrentKeyboardState.IsKeyDown(Keys.Escape) && Globals.LastKeyboardState.IsKeyUp(Keys.Escape))
            Globals.PAUSE = !Globals.PAUSE;

        Globals.LastKeyboardState = Globals.CurrentKeyboardState;

        if (Globals.PAUSE && !userInterface.GiftCardDisplayed)
            Globals.DISPLAY_PAUSE_MENU = true;
        else
            Globals.DISPLAY_PAUSE_MENU = false;
    }

    // Separate from the pause handling because that one stops listening the
    // moment the player dies or opens the menu, and the debug keys have to
    // keep working on the death screen - that is often exactly when you want
    // to switch one on.
    private void CheckDebugKeys()
    {
        DebugMode.Update(player, weaponsManager);
    }

    private void DrawDebugBoundingBoxes()
    {
        if (gameWorld == null || Globals.MENU)
            return;

        debugRenderer.pb.Begin(ref debugRenderer.proj, ref debugRenderer.view);
        Rectangle tmp;
        Circle tmpC;

        foreach (var p in gameWorld.GetCurrentLayerPlatformList())
        {
            tmp = p.bounds;
            tmp.X += (int)Globals.CameraOffset.X;
            tmp.Y += (int)Globals.CameraOffset.Y;
            debugRenderer.DrawRect(tmp, Color.Red);
        }

        // Every mob that is currently being simulated, not just this layer's
        foreach (var layer in gameWorld.ActiveLayers())
        {
            foreach (var m in layer.MobManager.mobs)
            {
                // Mobs with a real outline draw that instead of the box
                if (m.HitboxPolygon.HasValue)
                {
                    Polygon poly = m.HitboxPolygon.Value;
                    for (int i = 0; i < poly.Count; i++)
                    {
                        Vector2 a = poly[i] + Globals.CameraOffset;
                        Vector2 b = poly[(i + 1) % poly.Count] + Globals.CameraOffset;

                        debugRenderer.pb.AddVertex(a, Color.Yellow, PrimitiveType.LineList);
                        debugRenderer.pb.AddVertex(b, Color.Yellow, PrimitiveType.LineList);
                    }
                    continue;
                }

                tmp = m.Bounds;
                tmp.X += (int)Globals.CameraOffset.X;
                tmp.Y += (int)Globals.CameraOffset.Y;
                debugRenderer.DrawRect(tmp, Color.Blue);
            }
        }
        foreach (var b in weaponsManager.Bullets)
        {
            tmpC = b.bulletBounds;
            tmpC.X += (int)Globals.CameraOffset.X;
            tmpC.Y += (int)Globals.CameraOffset.Y;
            debugRenderer.DrawCircle(tmpC, Color.Red);
        }
        tmp = player.PlayerBounds;
        tmp.X += (int)Globals.CameraOffset.X;
        tmp.Y += (int)Globals.CameraOffset.Y;
        debugRenderer.DrawRect(tmp, Color.Green);
        debugRenderer.pb.End();
    }
}
