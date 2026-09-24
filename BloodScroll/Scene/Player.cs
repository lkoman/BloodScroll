using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// PLAYER - LOAD, UPDATE, DRAW
// HANDLE INPUT
//

public class Player : IPlayer, IDrawableLayer
{
    public int DrawLayer { get; set; } = 30;

    // INTERFACE VARIABLES
    public int HP 
    { 
        get => playerHP; 
        set => playerHP = value; 
    }
    public int MaxHP => playerMaxHP;
    public Vector2 Position => _player.Position;
    public float Bottom => _player.Bottom;
    public float Height => _player.Height;
    public float Width => _player.Width;
    public bool InAir => inAir;

    // MAX JUMP - highest the player can get off a platform. Caps how far apart
    // the generator may stack them.
    public static readonly float PLAYER_MAX_JUMP_Y;

    // The generator needs this before any player exists, so the sprite hands it
    // over on load. The value here is the fallback until then.
    public static float PLAYER_HEIGHT { get; private set; } = 124f;

    private AnimatedSprite _player;
    private AnimatedSprite _player_idle, _player_running, _player_in_jump;
    private Vector2 prevPos;

    //
    // TWO HITBOXES
    //
    //   HURT BOX  - the body. What every mob, shot and blast is tested against.
    //   FOOTING   - only as wide as the tendrils. Platform code only.
    //
    // Fractions of the sprite frame, same as MobBase.HitboxShape -
    // (0.5f, 0f) is top centre.
    //

    // Convex (SAT needs it) and lopsided - the hood sits right of centre, so it
    // mirrors with the sprite (see SetMirrored, facingLeft below).
    // Traced over all seven frames. The ragged cloak is left OUTSIDE.
    private static readonly Vector2[] HURTBOX_SHAPE =
    [
        new(0.46f, 0.00f),   // the point of the hood
        new(0.60f, 0.01f),
        new(0.78f, 0.14f),
        new(0.87f, 0.33f),   // the brow, the widest he ever gets
        new(0.87f, 0.74f),
        new(0.70f, 0.96f),
        new(0.40f, 1.00f),   // the tendrils
        new(0.26f, 0.84f),
        new(0.21f, 0.40f),
        new(0.29f, 0.13f),
    ];

    // THE COLUMN HE STANDS IN. Tendrils never leave x 0.29..0.73 of the frame.
    // FULL FRAME HEIGHT - the platform code measures from the frame's bottom
    // edge (PlacePlayerOnPlatform, the prevPos tests below). Only width narrows.
    private const float FOOTING_LEFT = 0.30f;
    private const float FOOTING_RIGHT = 0.70f;

    // Kept HERE, not read off the sprite: Draw swaps between three sprites and
    // each carries its own Effects, so the current one may be stale.
    private bool facingLeft = false;

    private Polygon hurtBox;
    private Rectangle footingBounds;

    public Polygon HurtBox => hurtBox;
    public Rectangle FootingBounds => footingBounds;
    private readonly float playerStartHeight = Core.windowHeight - 300;

    private const int StartPlayerHP = 500;
    private int playerMaxHP = StartPlayerHP; // Max HP of player (player gets this amount of HP when beating a boss)
    private int playerHP = StartPlayerHP; // Current player HP
    
    // PLAYER MOVEMENT
    private const float SPEED = 100.0f;
    private const float MAX_SPEED = 600.0f;
    private const float JUMP = 800.0f; 
    private const float GRAVITY = 9.81f * 200;
    private const float SPRINT_ACCEL = 1.5f;

    //
    // THE FASTEST HE MAY EVER FALL
    //
    // A landing is only noticed if his feet were within ONE PLATFORM'S HEIGHT
    // of the ledge on the previous frame - see IsPlayerStandingOnPlatform, which
    // measures prevPos against a window that wide. Move further than that in a
    // single frame and the test is stepped clean over.
    //
    // Falling from any height built up more than that, so a long drop went
    // THROUGH every small ledge on the way down and only stopped at the ground
    // slab, which is thick enough to still catch him. The cap is therefore
    // exactly one platform height per frame: the fastest speed at which no
    // ledge can be missed.
    //
    // TAKEN FROM THE ART, not written down as a number - a thinner platform
    // would silently need a lower cap, and this way it gets one.
    //
    // It only bites after about a screen of falling. An ordinary jump between
    // two ledges never comes close to it, so nothing about the normal feel of
    // the game moves.
    //
    private static float MaxFallSpeed => LayerGenerator.SmallPlatformHeight / Globals.DT;

    private float speed, max_speed, jump = 0f;
    private bool canJump = true;
    private Vector2 velocity = new (0f, 0f);
    public bool inAir = true;

    // HIT TIMER
    private bool isHit = false;
    private float hitTimer = 0f;
    private const float hitDuration = 0.15f; // seconds

    // SLOW (spider webs) - multiplies speed and jump while it lasts
    public const float WEB_SLOW_FACTOR = 0.45f;
    public const float WEB_SLOW_SECONDS = 2.5f;
    private float slowFactor = 1f;
    private float slowTimer = 0f;
    public bool IsSlowed => slowTimer > 0f;

    // The sprite washes pale and the strands sit over it, both fading over the
    // last stretch of the timer.
    private const float WEB_FADE_SECONDS = 0.6f;
    private const float WEB_PALE = 0.45f;   // how far towards white the sprite washes
    private readonly WebOverlay webOverlay = new();

    //
    // POISON (the green bat)
    //
    // Drains 1 HP/s for 20s and STOPS AT POISON_FLOOR - it can never kill.
    //
    // IT STACKS, AND EVERY BITE IS ITS OWN CLOCK. Two bats = 2 HP/s.
    // Kept as separate doses, not one rate + one timer: folding them would let
    // a late bite stretch the whole stack out to the new deadline.
    private readonly record struct PoisonDose(float PerSecond, float SecondsLeft);

    public const int POISON_FLOOR = 10;
    private readonly List<PoisonDose> poisonDoses = [];
    private float poisonBuffer = 0f;    // leftover fraction of a point between frames
    private float poisonPulse = 0f;     // only drives the flashing, see DrawPoison
    public bool IsPoisoned => poisonDoses.Count > 0;

    // The sprite washed towards the bat's green. Deepens per bite, capped.
    private const float POISON_PALE = 0.3f;
    private const float POISON_PALE_MAX = 0.6f;

    //
    // ROOTED (the green spider's web)
    //
    // Not slowed - STUCK. No steering, no jumping, for one second.
    private float rootTimer = 0f;
    public bool IsRooted => rootTimer > 0f;

    // KNOCKBACK (the moth's wing blast). No steering at all while it runs.
    private const float KNOCKBACK_SECONDS = 0.35f;
    private const float KNOCKBACK_DRAG = 0.94f;   // per frame, so the throw eases off
    private float knockbackTimer = 0f;

    //
    // DASH - Q left, E right
    //
    // A short, flat burst: gravity is held off while it runs, so it works as an
    // air dash across a gap as well as a sidestep on the ground. Keyboard is dead
    // for its duration, same as a knockback. A web shortens it like it slows
    // everything else.
    private const float DASH_SPEED = 1800f;
    private const float DASH_SECONDS = 0.14f;
    private const float DASH_COOLDOWN = 1.5f;
    private float dashTimer = 0f;
    private float dashCooldown = 0f;
    private float dashDirection = 0f;
    private bool canDash = true;

    // 0 right after a dash, 1 when it can be used again
    public float DashReady => 1f - dashCooldown / DASH_COOLDOWN;

    // GIFTS won from bosses
    private int lifeSteal = 0;      // HP returned per kill
    private int shield = 0;         // soaks damage before HP does
    private int shieldMax = 0;
    private int maxJumps = 1;       // 2 once the double jump is won
    private int jumpsUsed = 0;

    // SHIELD REGEN. Any hit holds it; breaking the shield holds it much longer.
    private const float SHIELD_REGEN_PER_SECOND = 0.15f;  // share of the capacity per second
    private const float SHIELD_HIT_DELAY = 2f;            // seconds on hold after any hit
    private const float SHIELD_BREAK_DELAY = 5f;          // seconds on hold after it drops to zero
    private float shieldRegenDelay = 0f;
    private float shieldRegenBuffer = 0f;                 // leftover fraction of a point between frames

    private readonly ShieldAura shieldAura = new();

    public int Shield => shield;
    public int ShieldMax => shieldMax;

    // 0 when there is no shield or it is broken, 1 when it is full
    public float ShieldFill => shieldMax > 0 ? shield / (float)shieldMax : 0f;
    public bool HasDoubleJump => maxJumps > 1;
    public bool CanJumpAgain => jumpsUsed < maxJumps;

    // Weapons
    private bool canSwitchWeapon = true;

    //
    // THE FLOWER BOMB
    //
    // wantsInteract is set on the frame E goes down and cleared at the start of
    // the next update. Everything asks through TryTakeInteract, first to ask
    // gets it.
    private bool hasBomb = false;
    private bool wantsInteract = false;
    private bool canInteract = true;
    public bool HasBomb => hasBomb;

    // The plucked head, drawn over the player
    private Sprite _heldBomb;
    private const float HELD_BOMB_SCALE = 0.55f;
    private const float HELD_BOMB_GAP = 8f;

    static Player()
    {
        float dt = 1f / 60f;

        int N = (int)Math.Ceiling(JUMP / (GRAVITY * dt));
        PLAYER_MAX_JUMP_Y = (float)(-dt * ( N * (-JUMP) + GRAVITY * dt * N * (N + 1) / 2.0 ));
    }

    // How far the player gets sideways on a jump that must also gain `rise`
    // pixels of height. A flat jump is the widest; a jump to the top of the arc
    // gets half of it.
    public static float MaxJumpRun(float rise)
    {
        // What is left of the take off speed once he has climbed `rise`
        float speedLeft = JUMP * JUMP - 2f * GRAVITY * rise;

        if (speedLeft <= 0f)
            return 0f;

        // The last moment he is still that high, on the way back down
        float airTime = (JUMP + (float)Math.Sqrt(speedLeft)) / GRAVITY;

        return MAX_SPEED * airTime;
    }

    public void Restart()
    {
        playerMaxHP = StartPlayerHP;
        playerHP = StartPlayerHP;

        _player = _player_idle;
        _player.Position = new Vector2(Core.windowWidth / 2 - _player.Width / 2, playerStartHeight);
        BuildHitboxes();

        speed = 0f; max_speed = 0f; jump = 0f;
        facingLeft = false;
        canJump = true;
        velocity = new (0f, 0f);
        SetPlayerInAir(true);

        slowFactor = 1f;
        slowTimer = 0f;
        knockbackTimer = 0f;
        dashTimer = 0f;
        dashCooldown = 0f;
        canDash = true;
        isHit = false;
        hitTimer = 0f;

        poisonDoses.Clear();
        poisonBuffer = 0f;
        poisonPulse = 0f;
        rootTimer = 0f;

        // Gifts are earned again from scratch every run
        lifeSteal = 0;
        shield = 0;
        shieldMax = 0;
        shieldRegenDelay = 0f;
        shieldRegenBuffer = 0f;
        maxJumps = 1;
        jumpsUsed = 0;

        hasBomb = false;
        wantsInteract = false;
        canInteract = true;
    }

    public void LoadContent()
    {
        _player_idle = Globals.Player.CreateAnimatedSprite("player-idle");
        _player_running = Globals.Player.CreateAnimatedSprite("player-running");
        _player_in_jump = Globals.Player.CreateAnimatedSprite("player-in-jump");

        _player = _player_idle;
        _player.Position = new Vector2(Core.windowWidth / 2 - _player.Width / 2, playerStartHeight);
        BuildHitboxes();

        PLAYER_HEIGHT = _player.Height;

        // The bomb IS the flower head - it is the thing that was pulled off
        // the stalk, so it is drawn as exactly that
        _heldBomb = Globals.Flower.CreateSprite("Flower_face");
        _heldBomb.Scale = new Vector2(HELD_BOMB_SCALE, HELD_BOMB_SCALE);

        shieldAura.LoadContent(_player.Width, _player.Height);
        webOverlay.LoadContent(_player.Width, _player.Height);
    }

    public void Update(IAudioService audio, IWeaponsManager weaponsManager)
    {
        // One frame only
        wantsInteract = false;

        if (Globals.PLAYER_ALIVE) {
            CheckKeyboardInput(audio, weaponsManager);
            UpdateShieldRegen();
        }
        else {
            velocity.X = 0;
            velocity.Y = 0;
        }

        UpdateHitTimer();
        UpdateDashCooldown();
        UpdateSlowTimer();
        UpdatePoison();
        UpdateRoot();

        // EVERY WAY GRAVITY GETS APPLIED FUNNELS THROUGH HERE, whether it came
        // from the ordinary walk, from being rooted, or from a wing blast - so
        // this is the one place the fall has to be held back
        CapFall();

        prevPos = _player.Position;
        _player.Position += velocity * Globals.DT;

        if (PlayerHitEdge()) {
            // Player fall down the edge
            _player.Position = prevPos;
            velocity.X = 0;
            if (inAir)
                velocity.Y += GRAVITY * Globals.DT;

            // The line above is another frame of gravity, so the cap is
            // re-applied before it is allowed to move him
            CapFall();

            _player.Position += velocity * Globals.DT;
        }

        SyncHitboxes();

        _player.Update();

        if (playerHP <= 0) {
            Globals.PLAYER_ALIVE = false;
        }
    }

    // Cut once - all three animations share one frame size, so the outline only
    // ever needs moving after this
    private void BuildHitboxes()
    {
        hurtBox = Polygon.FromFractions(HURTBOX_SHAPE, _player.Width, _player.Height);
        SyncHitboxes();
    }

    // Both follow the sprite every frame, from one place
    private void SyncHitboxes()
    {
        hurtBox.SetPosition(_player.Position);
        hurtBox.SetMirrored(facingLeft, _player.Width);

        footingBounds = new Rectangle(
            (int)(_player.Position.X + _player.Width * FOOTING_LEFT),
            (int)_player.Position.Y,
            (int)(_player.Width * (FOOTING_RIGHT - FOOTING_LEFT)),
            (int)_player.Height);
    }

    public void Draw()
    {
        if (inAir) {
            _player_in_jump.Position = _player.Position;
            _player = _player_in_jump;
        }
        else if (velocity.X != 0) {
            _player_running.Position = _player.Position;
            _player = _player_running;
        }
        else {
            _player_idle.Position = _player.Position;
            _player = _player_idle;
        }

        // AFTER the swap, and every frame: the sprite coming on screen was last
        // turned whenever it was last used, which may be stale
        _player.Effects = facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        Color drawColor = Color.White;
        if (isHit)
            drawColor = Globals.Red;

        _player.Draw(drawColor);

        Vector2 center = _player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f);

        DrawPoison();
        DrawWeb(center);

        if (hasBomb)
            DrawHeldBomb();

        // On top of the sprite. Fades with the shield, gone at zero.
        shieldAura.Draw(center, ShieldFill);
    }

    // Poisoned: the same wash the web uses, in the bat's green. Pulses.
    private void DrawPoison()
    {
        if (poisonDoses.Count == 0)
            return;

        float pulse = 0.6f + 0.4f * MathF.Sin(poisonPulse * 6f);

        // Deeper per bite, capped so a swarm does not hide the sprite
        float depth = MathF.Min(POISON_PALE * poisonDoses.Count, POISON_PALE_MAX);

        _player.Draw(Globals.PoisonGreen * (depth * pulse));
    }

    // Caught in a web: the sprite painted white over itself, strands on top
    private void DrawWeb(Vector2 center)
    {
        if (slowTimer <= 0f)
            return;

        float strength = MathHelper.Clamp(slowTimer / WEB_FADE_SECONDS, 0f, 1f);

        _player.Draw(Color.White * (WEB_PALE * strength));

        // The timer doubles as the clock - the game has no global one
        float shimmer = 0.85f + 0.15f * MathF.Sin(slowTimer * 10f);
        webOverlay.Draw(center, strength * shimmer);
    }

    private void CheckKeyboardInput(IAudioService audio, IWeaponsManager weaponsManager)
    {
        // Mid flight after a wing blast - keyboard is dead until he lands
        if (UpdateKnockback())
            return;

        // NAILED TO THE SPOT. No steering, but gravity still applies.
        if (IsRooted)
        {
            dashTimer = 0f;
            velocity.X = 0;

            if (inAir)
                velocity.Y += GRAVITY * Globals.DT;

            return;
        }

        KeyboardState keyboardState = Keyboard.GetState();

        // Mid dash - nothing else is read until it ends
        if (UpdateDash(keyboardState, audio))
            return;

        speed = SPEED;
        max_speed = MAX_SPEED;
        jump = JUMP;

        // AFTER the constants are restored, or it gets wiped every frame.
        // Only sideways movement - the generator assumes a full height jump.
        if (slowTimer > 0f)
        {
            speed *= slowFactor;
            max_speed *= slowFactor;
        }

        // INTERACT - pluck a flower, or put down the bomb in hand.
        // Press only, never hold, or a dropped bomb is picked straight back up.
        if (keyboardState.IsKeyDown(Keys.F) && canInteract)
        {
            wantsInteract = true;
            canInteract = false;
        }
        else if (keyboardState.IsKeyUp(Keys.F))
        {
            canInteract = true;
        }

        // WEAPON SWITCH. G, not W - W sits under the movement keys.
        if (keyboardState.IsKeyDown(Keys.G) && canSwitchWeapon)
        {
            weaponsManager.SwitchWeapon();
            canSwitchWeapon = false;
        }
        else if (keyboardState.IsKeyUp(Keys.G))
        {
            canSwitchWeapon = true;
        }

        // The wheel does the same and walks both ways. No press/release guard -
        // a notch is already one event, not a held key.
        int wheelDelta = Globals.MouseState.ScrollWheelValue - Globals.LastMouseState.ScrollWheelValue;
        if (wheelDelta != 0)
            weaponsManager.SwitchWeapon(wheelDelta > 0 ? 1 : -1);

        // SPRINT
        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
        {
            speed *= SPRINT_ACCEL;
            max_speed *= SPRINT_ACCEL;
        }

        // LEVO
        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
        {
            facingLeft = true;
            velocity.X -= speed;
        }
        // DESNO
        else if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
        {
            facingLeft = false;
            velocity.X += speed;
        }
        // PREVENT SLIDE
        else velocity.X = 0;

        // JUMP
        if (keyboardState.IsKeyDown(Keys.Space) && canJump && jumpsUsed < maxJumps)
        {
            audio.PlaySound(AudioId.PlayerJump);

            // Assigned, not subtracted, so a mid air jump is the same whether
            // you were rising or falling
            velocity.Y = -jump;
            jumpsUsed ++;

            SetPlayerInAir(true);
            canJump = false;
        }
        if (keyboardState.IsKeyUp(Keys.Space))
        {
            canJump = true;
        }
        if (inAir)
        {
            velocity.Y += GRAVITY * Globals.DT;
        }

        velocity.X = Math.Clamp(velocity.X, -max_speed, max_speed);
    }

    // DOWNWARDS ONLY. Being thrown UP by a wing blast is not a fall and has
    // nothing to tunnel through - the landing test only fires while he is on
    // the way back down.
    private void CapFall()
    {
        if (velocity.Y > MaxFallSpeed)
            velocity.Y = MaxFallSpeed;
    }

    //
    // THE DASH
    //
    // Returns true while a dash is running. Otherwise checks for a fresh press
    // of Q or E and starts one if the cooldown has run out. Press only, never
    // hold, so a held key does not fire again the moment the cooldown ends.
    //
    private bool UpdateDash(KeyboardState keyboardState, IAudioService audio)
    {
        if (dashTimer > 0f)
        {
            dashTimer -= Globals.DT;

            velocity.X = dashDirection * DASH_SPEED * slowFactor;
            velocity.Y = 0f;

            // Let go of the throw at the end, so he does not carry the burst
            // on into the next frame's steering
            if (dashTimer <= 0f)
                velocity.X = dashDirection * MAX_SPEED * slowFactor;

            return true;
        }

        bool left = keyboardState.IsKeyDown(Keys.Q);
        bool right = keyboardState.IsKeyDown(Keys.E);

        if (!left && !right)
        {
            canDash = true;
            return false;
        }

        if (!canDash || dashCooldown > 0f || (left && right))
            return false;

        canDash = false;
        dashDirection = left ? -1f : 1f;
        facingLeft = left;
        dashTimer = DASH_SECONDS;
        dashCooldown = DASH_COOLDOWN;

        audio.PlaySound(AudioId.PlayerJump);

        velocity.X = dashDirection * DASH_SPEED * slowFactor;
        velocity.Y = 0f;

        return true;
    }

    private void UpdateDashCooldown()
    {
        if (dashCooldown > 0f)
            dashCooldown -= Globals.DT;
    }

    // Hit for changing color when player is hit
    private void UpdateHitTimer()
    {
        if (isHit)
        {
            hitTimer -= Globals.DT;
            if (hitTimer <= 0f)
                isHit = false;
        }
    }

    // Regen is a share of the capacity per second, so a big shield fills in the
    // same time as a small one
    private void UpdateShieldRegen()
    {
        if (shieldMax <= 0 || shield >= shieldMax)
            return;

        if (shieldRegenDelay > 0f)
        {
            shieldRegenDelay -= Globals.DT;
            return;
        }

        shieldRegenBuffer += shieldMax * SHIELD_REGEN_PER_SECOND * Globals.DT;

        // Whole points only, the rest waits for the next frame
        int points = (int)shieldRegenBuffer;
        if (points > 0)
        {
            shield = Math.Min(shield + points, shieldMax);
            shieldRegenBuffer -= points;
        }
    }

    private void UpdateSlowTimer()
    {
        if (slowTimer <= 0f)
            return;

        slowTimer -= Globals.DT;
        if (slowTimer <= 0f)
            slowFactor = 1f;
    }

    // Goes STRAIGHT TO HP - the shield does not soak it and it does not stall
    // the shield regen, because it is not a hit.
    //
    // Stops dead at POISON_FLOOR. Below that the doses keep running (green stays
    // on, clocks keep counting) but take nothing.
    private void UpdatePoison()
    {
        if (poisonDoses.Count == 0)
        {
            poisonBuffer = 0f;
            return;
        }

        poisonPulse += Globals.DT;

        // Every bite still running adds its own rate on top
        float perSecond = 0f;

        for (int i = poisonDoses.Count - 1; i >= 0; i--)
        {
            PoisonDose dose = poisonDoses[i] with { SecondsLeft = poisonDoses[i].SecondsLeft - Globals.DT };

            if (dose.SecondsLeft <= 0f)
            {
                poisonDoses.RemoveAt(i);
                continue;
            }

            poisonDoses[i] = dose;
            perSecond += dose.PerSecond;
        }

        if (playerHP <= POISON_FLOOR)
            return;

        // DEBUG MODE (F2). The one HP drain that does not go through TakeDamage,
        // so it is checked again here. Doses still tick and still run out.
        if (DebugMode.Invulnerable)
            return;

        poisonBuffer += perSecond * Globals.DT;

        // Whole points only, the rest waits for the next frame
        int points = (int)poisonBuffer;
        if (points <= 0)
            return;

        poisonBuffer -= points;
        playerHP = Math.Max(playerHP - points, POISON_FLOOR);
    }

    private void UpdateRoot()
    {
        if (rootTimer <= 0f)
            return;

        rootTimer -= Globals.DT;
    }

    // Velocity is REPLACED, not added to, so a gust throws you the same way
    // whichever direction you were running
    public void Knockback(Vector2 direction, float force)
    {
        if (direction.LengthSquared() < 0.0001f)
            return;

        direction.Normalize();

        velocity = direction * force;
        knockbackTimer = KNOCKBACK_SECONDS;

        // A gust ends a dash outright, or it would pick up again afterwards
        dashTimer = 0f;

        // Counts as a spent jump, so the throw cannot be cancelled with a free
        // double jump the moment it lets go
        SetPlayerInAir(true);
    }

    // True while still tumbling. Gravity keeps pulling, the throw bleeds off,
    // nothing he presses matters until the timer runs out.
    private bool UpdateKnockback()
    {
        if (knockbackTimer <= 0f)
            return false;

        knockbackTimer -= Globals.DT;
        velocity.X *= KNOCKBACK_DRAG;

        if (inAir)
            velocity.Y += GRAVITY * Globals.DT;

        return true;
    }

    // Strongest slow wins, timer is always refreshed
    public void ApplySlow(float factor, float seconds)
    {
        slowFactor = Math.Min(slowFactor, factor);
        slowTimer = Math.Max(slowTimer, seconds);
    }

    // Every bite is added on top and runs out on its own clock
    public void ApplyPoison(int totalDamage, float seconds)
    {
        if (seconds <= 0f || totalDamage <= 0)
            return;

        poisonDoses.Add(new PoisonDose(totalDamage / seconds, seconds));
    }

    // Refreshed rather than added to, same as a slow
    public void Root(float seconds)
    {
        rootTimer = Math.Max(rootTimer, seconds);
    }

    // Every kill in the world reports here
    public void OnMobKilled()
    {
        if (lifeSteal > 0)
            Heal(lifeSteal);
    }

    //
    // THE FLOWER BOMB
    //

    // At most once per press - first caller gets it
    public bool TryTakeInteract()
    {
        if (!wantsInteract)
            return false;

        wantsInteract = false;
        return true;
    }

    public void GiveBomb() => hasBomb = true;
    public void UseBomb() => hasBomb = false;

    // Held over his head
    private void DrawHeldBomb()
    {
        _heldBomb.Position = new Vector2(
            _player.Position.X + (_player.Width - _heldBomb.Width) / 2f,
            _player.Position.Y - _heldBomb.Height - HELD_BOMB_GAP);

        _heldBomb.Draw();
    }

    private bool PlayerHitEdge()
    {
        return _player.Position.X > Core.windowWidth - _player.Width + Globals.CameraOffset.X ||
               _player.Position.X < Globals.CameraOffset.X;
    }

    public void Heal()
    {
        playerHP = playerMaxHP;
    }

    // Partial heal, never past the maximum
    public void Heal(int amount)
    {
        playerHP = Math.Min(playerHP + amount, playerMaxHP);
    }

    // A GIFT CAN ONLY EVER RAISE IT. Boss layers hand out both flat maximums and
    // increments, and they can arrive in an order where the flat one is lower.
    public void IncreaseMaxHP(int newMaxHP)
    {
        playerMaxHP = Math.Max(playerMaxHP, newMaxHP);
    }

    public void TakeDamage(int damage, IAudioService audio)
    {
        // DEBUG MODE (F2). The hit never happened - no shield soak, no regen
        // stall, no flash. Gusts and webs still work; they are not damage.
        if (DebugMode.Invulnerable)
            return;

        audio.PlaySound(AudioId.PlayerHit);

        isHit = true;
        hitTimer = hitDuration;

        // Every damage source funnels through here, so the shield is soaked in
        // this one place
        if (shield > 0)
        {
            int absorbed = Math.Min(shield, damage);
            shield -= absorbed;
            damage -= absorbed;
        }

        // Any hit stalls the regen; emptying the shield stalls it much longer
        if (shieldMax > 0)
        {
            shieldRegenBuffer = 0f;
            shieldRegenDelay = shield > 0 ? SHIELD_HIT_DELAY : SHIELD_BREAK_DELAY;
        }

        playerHP -= damage;
        if (playerHP <= 0) playerHP = 0;
    }

    // GIFTS
    public void GrantShield(int capacity)
    {
        shieldMax = capacity;
        shield = capacity; // arrives full
        shieldRegenDelay = 0f;
        shieldRegenBuffer = 0f;
    }

    public void GrantDoubleJump()
    {
        maxJumps = 2;
    }

    public void GrantLifeSteal(int hpPerKill)
    {
        lifeSteal = hpPerKill;
    }

    // Boss kills refill the shield along with the heal
    public void RefillShield()
    {
        shield = shieldMax;
        shieldRegenDelay = 0f;
        shieldRegenBuffer = 0f;
    }

    public void PlacePlayerOnPlatform(float platformY)
    {
        _player.Position = new Vector2(_player.Position.X, platformY - _player.Height);
        velocity.Y = 0;
        SetPlayerInAir(false);
    }

    public void SetPlayerInAir(bool b)
    {
        inAir = b;

        // Landing gives the jumps back. Walking off a ledge burns the first one,
        // or stepping into thin air would be a free extra jump.
        if (!b)
            jumpsUsed = 0;
        else if (jumpsUsed == 0)
            jumpsUsed = 1;
    }

    public bool IsPlayerStandingOnPlatform(Rectangle platform)
    {
        return 
            velocity.Y > 0 && // when velocity in Y is bigger then 0, the player is falling
            // Is players bottom on platform
            prevPos.Y + _player.Height <= platform.Y + platform.Height &&
            prevPos.Y + _player.Height >= platform.Y - platform.Height;
    }
}