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
    public Rectangle Bounds => playerBounds;
    public Vector2 Position => _player.Position;
    public float Bottom => _player.Bottom;
    public float Height => _player.Height;
    public float Width => _player.Width;
    public bool InAir => inAir;

    // MAX JUMP - the highest the player can ever get off a platform, which is
    // what caps how far apart the generator may stack them
    public static readonly float PLAYER_MAX_JUMP_Y;

    // The platform generator has to size its gaps for the player before any
    // player exists, so the sprite hands its height over as soon as it loads.
    // The fallback is only there for the frames before LoadContent runs.
    public static float PLAYER_HEIGHT { get; private set; } = 124f;

    private AnimatedSprite _player;
    private AnimatedSprite _player_idle, _player_running, _player_in_jump;
    private Rectangle playerBounds;
    public Rectangle PlayerBounds => playerBounds;
    private Vector2 prevPos;
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

    // How the web looks while it holds on: the sprite washes out pale and the
    // strands sit over it. Both fade over the last stretch of the timer, so the
    // player can see the web letting go instead of it just blinking off.
    private const float WEB_FADE_SECONDS = 0.6f;
    private const float WEB_PALE = 0.45f;   // how far towards white the sprite washes
    private readonly WebOverlay webOverlay = new();

    //
    // POISON (the green bat)
    //
    // Drains HP slowly - a single point a second, for twenty seconds - and
    // STOPS AT A FLOOR. It will take the player to within an inch of dying and
    // no further. That floor is the whole design of it: poison is meant to make
    // the next twenty seconds desperate, not to kill him while he is standing
    // in an empty room with nothing left to fight. Everything else in this game
    // kills you by hitting you, which is something you can see coming and step
    // out of.
    //
    // IT STACKS, AND EVERY BITE IS ITS OWN CLOCK
    //
    // Two bats on you is two points a second, three is three. It used to take
    // the worse of the two doses and throw the other away, which meant the
    // second green bat in a wave was free - now every one of them costs you.
    //
    // What makes that safe to do is the floor: however many of them land, the
    // poison still cannot take the last ten HP, so a swarm makes you desperate
    // faster rather than killing you outright. The bats themselves still can.
    //
    // Each bite is kept separately rather than being folded into one rate and
    // one timer, because folding them makes a bite landing late in the first
    // one's life stretch the WHOLE stack out to the new deadline.
    private readonly record struct PoisonDose(float PerSecond, float SecondsLeft);

    public const int POISON_FLOOR = 10;
    private readonly List<PoisonDose> poisonDoses = [];
    private float poisonBuffer = 0f;    // leftover fraction of a point between frames
    private float poisonPulse = 0f;     // only drives the flashing, see DrawPoison
    public bool IsPoisoned => poisonDoses.Count > 0;

    // How the poison looks: the sprite washed towards the sickly green the bat
    // that gave it to you is drawn in. Per bite, so a stack shows.
    private const float POISON_PALE = 0.3f;
    private const float POISON_PALE_MAX = 0.6f;

    //
    // ROOTED (the black spider's web)
    //
    // Not slowed - STUCK. No steering, no jumping, for one second. An ordinary
    // web is a tax on your movement that you play through; this one takes the
    // controls away, and the fight carries on without you.
    private float rootTimer = 0f;
    public bool IsRooted => rootTimer > 0f;

    // KNOCKBACK (the moth's wing blast)
    // While this runs the player has no steering at all. Being thrown across
    // the room is only frightening if you cannot just walk out of it - and
    // the platform you get thrown off was the whole point of the attack.
    private const float KNOCKBACK_SECONDS = 0.35f;
    private const float KNOCKBACK_DRAG = 0.94f;   // per frame, so the throw eases off
    private float knockbackTimer = 0f;

    // GIFTS won from bosses
    private int lifeSteal = 0;      // HP returned per kill
    private int shield = 0;         // soaks damage before HP does
    private int shieldMax = 0;
    private int maxJumps = 1;       // 2 once the double jump is won
    private int jumpsUsed = 0;

    // SHIELD REGEN
    // Getting hit puts the regen on hold, and breaking the shield holds it for
    // a lot longer - the shield is meant to reward not getting hit, so it must
    // never grow back in the middle of a fight you are losing.
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
    // the next update, so it can only ever be acted on once. Everything that
    // could react to it (the world putting a bomb down, a flower being pulled
    // up) asks for it through TryTakeInteract, and the first to ask gets it.
    private bool hasBomb = false;
    private bool wantsInteract = false;
    private bool canInteract = true;
    public bool HasBomb => hasBomb;

    // The plucked head, drawn over the player's own so it is obvious at a
    // glance that his hands are full and E will now put it down
    private Sprite _heldBomb;
    private const float HELD_BOMB_SCALE = 0.55f;
    private const float HELD_BOMB_GAP = 8f;

    static Player()
    {
        float dt = 1f / 60f;

        int N = (int)Math.Ceiling(JUMP / (GRAVITY * dt));
        PLAYER_MAX_JUMP_Y = (float)(-dt * ( N * (-JUMP) + GRAVITY * dt * N * (N + 1) / 2.0 ));
    }

    // How far the player gets sideways on a jump that also has to gain `rise`
    // pixels of height.
    //
    // A jump that lands where it took off is the widest there is - the whole
    // flight is spent going sideways. A jump that has to end higher
    // than it started only gets the part of the flight below that height, and a
    // jump to the very top of the arc gets half of it. Reading the two limits
    // apart is what puts the highest platforms behind the widest gaps.
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
        playerBounds = CollisionManager.SetBoundingRectangle(_player);

        speed = 0f; max_speed = 0f; jump = 0f;
        canJump = true;
        velocity = new (0f, 0f);
        SetPlayerInAir(true);

        slowFactor = 1f;
        slowTimer = 0f;
        knockbackTimer = 0f;
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
        playerBounds = CollisionManager.SetBoundingRectangle(_player);

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
        // One frame only. Whatever wanted it has had its chance by now.
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
        UpdateSlowTimer();
        UpdatePoison();
        UpdateRoot();

        prevPos = _player.Position;
        _player.Position += velocity * Globals.DT;

        if (PlayerHitEdge()) {
            // Player fall down the edge
            _player.Position = prevPos;
            velocity.X = 0;
            if (inAir)
                velocity.Y += GRAVITY * Globals.DT;

            _player.Position += velocity * Globals.DT;
        }

        playerBounds = CollisionManager.UpdateBoundingRectangle(playerBounds, _player);

        _player.Update();

        if (playerHP <= 0) {
            Globals.PLAYER_ALIVE = false;
        }
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

        Color drawColor = Color.White;
        if (isHit)
            drawColor = Globals.Red;

        _player.Draw(drawColor);

        Vector2 center = _player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f);

        DrawPoison();
        DrawWeb(center);

        if (hasBomb)
            DrawHeldBomb();

        // On top of the sprite, so the ring reads as a bubble the player sits inside.
        // It fades out with the shield on its own and disappears completely at zero.
        shieldAura.Draw(center, ShieldFill);
    }

    // Poisoned: the same trick the web uses, in the sickly green of the bat
    // that did it. It pulses, so the player can tell a poison that is running
    // out from one that has just landed without reading the HUD.
    private void DrawPoison()
    {
        if (poisonDoses.Count == 0)
            return;

        float pulse = 0.6f + 0.4f * MathF.Sin(poisonPulse * 6f);

        // Deeper the more bites are running, so a player carrying three of them
        // LOOKS like it without having to count anything in the HUD. Capped, or
        // a swarm would paint him solid green and hide the sprite entirely.
        float depth = MathF.Min(POISON_PALE * poisonDoses.Count, POISON_PALE_MAX);

        _player.Draw(Globals.PoisonGreen * (depth * pulse));
    }

    // Caught in a web: the same sprite painted white over itself washes the
    // player out without touching the artwork, then the strands go on top
    private void DrawWeb(Vector2 center)
    {
        if (slowTimer <= 0f)
            return;

        float strength = MathHelper.Clamp(slowTimer / WEB_FADE_SECONDS, 0f, 1f);

        _player.Draw(Color.White * (WEB_PALE * strength));

        // A slow shimmer while it holds. The timer doubles as the clock - it
        // runs down at a fixed rate, and the game has no global one.
        float shimmer = 0.85f + 0.15f * MathF.Sin(slowTimer * 10f);
        webOverlay.Draw(center, strength * shimmer);
    }

    private void CheckKeyboardInput(IAudioService audio, IWeaponsManager weaponsManager)
    {
        // Mid flight after a wing blast - the keyboard is dead until he lands
        if (UpdateKnockback())
            return;

        // NAILED TO THE SPOT by a black web. Sideways movement is killed and
        // nothing he presses moves him, but gravity still applies - being stuck
        // in mid air would have him hanging there like a picture.
        if (IsRooted)
        {
            velocity.X = 0;

            if (inAir)
                velocity.Y += GRAVITY * Globals.DT;

            return;
        }

        KeyboardState keyboardState = Keyboard.GetState();
        speed = SPEED;
        max_speed = MAX_SPEED;
        jump = JUMP;

        // Webs bite AFTER the constants are restored, otherwise they get wiped every frame.
        // Only sideways movement is slowed - nerfing the jump too would put platforms
        // out of reach, and the generator assumes a full height jump.
        if (slowTimer > 0f)
        {
            speed *= slowFactor;
            max_speed *= slowFactor;
        }

        // INTERACT - pluck a flower, or put down the bomb already in hand.
        // Only the press counts, never the hold: a bomb put down would
        // otherwise be plucked back up the same second.
        if (keyboardState.IsKeyDown(Keys.E) && canInteract)
        {
            wantsInteract = true;
            canInteract = false;
        }
        else if (keyboardState.IsKeyUp(Keys.E))
        {
            canInteract = true;
        }

        // WEAPON SWITCH
        // G, not W - W sits under a finger that is on the movement keys, and a
        // gun swapped by accident mid fight is a gun fired by accident
        if (keyboardState.IsKeyDown(Keys.G) && canSwitchWeapon)
        {
            weaponsManager.SwitchWeapon();
            canSwitchWeapon = false;
        }
        else if (keyboardState.IsKeyUp(Keys.G))
        {
            canSwitchWeapon = true;
        }

        // SPRINT
        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
        {
            speed *= SPRINT_ACCEL;
            max_speed *= SPRINT_ACCEL;
        }

        // LEVO
        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
        {
            _player.Effects = SpriteEffects.FlipHorizontally;
            velocity.X -= speed;
        }
        // DESNO
        else if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
        {
            _player.Effects = SpriteEffects.None;
            velocity.X += speed;
        }
        // PREVENT SLIDE
        else velocity.X = 0;

        // JUMP
        if (keyboardState.IsKeyDown(Keys.Space) && canJump && jumpsUsed < maxJumps)
        {
            audio.PlaySound(AudioId.PlayerJump);

            // Assigned, not subtracted, so a mid air jump feels the same
            // whether you were rising or already falling
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

        velocity.X = MyMath.Clamp(velocity.X, -max_speed, max_speed);
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

    // The shield grows back by itself once the player has been left alone
    // long enough. Regen is a share of the capacity per second, so a bigger
    // shield does not feel slower to fill than a small one.
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

        // Whole points move to the shield, the rest waits for the next frame
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

    // The poison ticking away. It goes STRAIGHT TO HP: the shield does not
    // soak it and it does not stall the shield regen, because it is not a hit -
    // there is nothing to block, it is already inside him.
    //
    // And it stops dead at the floor. Below that the doses keep running (the
    // green stays on, the clocks keep counting) but they take nothing, so a
    // poisoned player at ten HP is a player in serious trouble rather than a
    // dead one.
    private void UpdatePoison()
    {
        if (poisonDoses.Count == 0)
        {
            poisonBuffer = 0f;
            return;
        }

        poisonPulse += Globals.DT;

        // Every bite still running adds its own point a second on top
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

        poisonBuffer += perSecond * Globals.DT;

        // Whole points come off, the rest waits for the next frame
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

    // Blown off your feet. The velocity is REPLACED rather than added to, so a
    // gust always throws you the same way whichever direction you were running
    // when it caught you.
    public void Knockback(Vector2 direction, float force)
    {
        if (direction.LengthSquared() < 0.0001f)
            return;

        direction.Normalize();

        velocity = direction * force;
        knockbackTimer = KNOCKBACK_SECONDS;

        // Counts as having spent a jump, so the throw cannot be cancelled with
        // a free double jump the moment it lets go
        SetPlayerInAir(true);
    }

    // True while he is still tumbling. Gravity keeps pulling and the throw
    // bleeds off, but nothing he presses matters until the timer runs out.
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

    // Stepping in a web. The strongest slow currently on the player wins,
    // and the timer is always refreshed so standing in a web keeps you stuck.
    public void ApplySlow(float factor, float seconds)
    {
        slowFactor = Math.Min(slowFactor, factor);
        slowTimer = Math.Max(slowTimer, seconds);
    }

    // Bitten by a green bat. Every bite is added on top of whatever is already
    // in him and runs out on its own clock, so two bats really is twice the
    // drain - the floor is what stops that from being an automatic death.
    public void ApplyPoison(int totalDamage, float seconds)
    {
        if (seconds <= 0f || totalDamage <= 0)
            return;

        poisonDoses.Add(new PoisonDose(totalDamage / seconds, seconds));
    }

    // Caught in a black web. Refreshed rather than added to, same as a slow.
    public void Root(float seconds)
    {
        rootTimer = Math.Max(rootTimer, seconds);
    }

    // Every kill anywhere in the world reports here
    public void OnMobKilled()
    {
        if (lifeSteal > 0)
            Heal(lifeSteal);
    }

    //
    // THE FLOWER BOMB
    //

    // Handed out at most once per press. Whoever calls first gets it, and
    // everyone after that this frame is told no.
    public bool TryTakeInteract()
    {
        if (!wantsInteract)
            return false;

        wantsInteract = false;
        return true;
    }

    public void GiveBomb() => hasBomb = true;
    public void UseBomb() => hasBomb = false;

    // Held over his head, so it is clear at a glance that E is now a "put it
    // down" key rather than a "pick one up" key
    private void DrawHeldBomb()
    {
        _heldBomb.Position = new Vector2(
            _player.Position.X + (_player.Width - _heldBomb.Width) / 2f,
            _player.Position.Y - _heldBomb.Height - HELD_BOMB_GAP);

        _heldBomb.Draw();
    }

    private bool PlayerHitEdge()
    {
        if (_player.Position.X > Core.windowWidth - _player.Width + Globals.CameraOffset.X ||
            _player.Position.X < 0 + Globals.CameraOffset.X)
        {
            return true;
        }
        return false;
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

    public void IncreaseMaxHP(int newMaxHP)
    {
        playerMaxHP = newMaxHP;
    }

    public void TakeDamage(int damage, IAudioService audio)
    {
        audio.PlaySound(AudioId.PlayerHit);

        isHit = true;
        hitTimer = hitDuration;

        // The shield soaks what it can before any of it reaches HP.
        // Every damage source in the game funnels through here, so this is
        // the only place that needs to know about it.
        if (shield > 0)
        {
            int absorbed = Math.Min(shield, damage);
            shield -= absorbed;
            damage -= absorbed;
        }

        // Any hit stalls the regen, and a hit that empties the shield stalls it
        // for much longer - that pause is the price of letting it come back at all
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
        // otherwise stepping into thin air would hand you a free extra jump.
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