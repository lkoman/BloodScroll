using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// SHARED BASE FOR EVERY MOB
//
// Red hit-flash timer, random direction timer, spawning, floor bounce, draw.
// A subclass writes LoadContent + UpdateBehaviour and overrides the rest.
//
// UP IS NEGATIVE. A mob on layer N lives around y = -windowHeight * N,
// which is what LayerTopY gives you.
//

public abstract class MobBase : IMob
{
    public int DrawLayer { get; set; } = 20;

    public Rectangle Bounds { get; set; }
    public int HP { get; set; }
    public int MaxHP { get; private set; }
    public int DAMAGE { get; set; }
    public int PointsOnKill { get; set; }
    public bool HittingPlayer { get; set; } = false;
    public OnTouch ON_TOUCH { get; set; } = OnTouch.HurtPlayer;

    // What the base class draws and builds the bounding box from. Mobs that swap
    // animations reassign this.
    protected AnimatedSprite Sprite;

    protected int SpawnLayer;
    protected Vector2 velocity;
    protected float speed, max_speed;

    // Radians, about the middle of the frame. Most mobs leave it alone and flip
    // instead. The outline hitbox turns with it.
    protected float SpriteRotation = 0f;

    // Still flashes and makes a noise when shot, just loses no HP
    // (the moth while it hides in its cocoon)
    protected bool Invulnerable = false;

    // A BOSS IS THE MOB THAT DRAWS ITS HP OVER ITS HEAD. One flag, so "ends the
    // room" and "scales with the climb" can never drift apart.
    public virtual bool IsBoss => ShowBossHP;

    // Bosses draw their remaining HP above their head
    protected virtual bool ShowBossHP => false;

    // Has to die before the layer is cleared. False for pickups and ambushes.
    public virtual bool CountsAsEnemy => true;

    // False for the two mobs set off by touching, so shots pass through them
    public virtual bool StopsBullets => true;

    // Those same two are not player kills, so no life steal
    public virtual bool GivesLifeSteal => true;

    // Sound played when this mob is shot. Null means silent.
    protected virtual AudioId? HitSound => AudioId.BatSqueak;

    // HIT TIMER
    protected bool isHit = false;
    private float hitTimer = 0f;
    private const float hitDuration = 0.15f; // seconds

    // STUN TIMER. A stunned mob does not move, aim, or tick its state machine.
    // It keeps its hitbox and still hurts to walk into.
    private float stunTimer = 0f;
    protected bool Stunned => stunTimer > 0f;

    // THE FREEZE BREAKING. Being hit by a mob that snaps back to full speed out
    // of nowhere is not fair, so the last stretch of the stun is TELEGRAPHED and
    // the mob then THAWS instead of jumping.
    //
    // 1. WARNING - the blue blinks off and on, faster the closer it gets
    // 2. THAW    - it moves again, at a fraction of its speed, ramped back to
    //              full while the blue drains out of it
    private const float STUN_WARNING = 0.7f;      // blinking starts this long before the end
    private const float STUN_FLICKER_SLOW = 5f;   // blinks per second when the warning starts
    private const float STUN_FLICKER_FAST = 18f;  // ...and just before the freeze breaks
    private const float STUN_THAW = 0.45f;        // slow motion after the freeze ends
    private const float STUN_THAW_START = 0.15f;  // share of its speed the moment it wakes

    // Counts DOWN like the stun. Everything the mob does is run on a scaled DT
    // while it is above zero.
    private float thawTimer = 0f;

    // Blinks per second change as the stun runs out, so the phase has to be
    // ACCUMULATED - a blink read straight off the remaining time would jump
    // every time the rate changed.
    private float flickerPhase = 0f;

    // Half of each blink the blue is off
    private bool StunFlickerOff => stunTimer <= STUN_WARNING && flickerPhase % 1f >= 0.5f;

    // KNOCKBACK. Pixels per second, bled off every frame. Only the rifle deals
    // any - see WeaponSpec.Knockback.
    //
    // A DISPLACEMENT laid over what the mob was doing, not a change to its own
    // velocity - a self steering mob would just steer the push out again.
    private Vector2 knockback = Vector2.Zero;

    // The mob travels roughly force/decay pixels total, so at 7 a hit of 800
    // moves it a little over a hundred
    private const float KNOCKBACK_DECAY = 7f;

    // Below this it is a pixel a second and not worth the arithmetic
    private const float KNOCKBACK_CUTOFF = 4f;

    // A BOSS IS HEAVY - same shove, a third of the distance
    private const float BOSS_KNOCKBACK_SHARE = 0.35f;

    // DIRECTION TIMER
    private float directionTimer = 1.5f;
    private float directionSeconds = 1.5f;

    // Y of the top of this mob's layer, in world space
    protected float LayerTopY => -Core.windowHeight * SpawnLayer;

    public abstract void LoadContent(Vector2 playerPos, int spawnLayer);

    public void Update(IPlayer player, GameWorld gameWorld)
    {
        UpdateHitTimer();

        // BEFORE the mob moves and before the stun check - being shoved is not
        // the mob acting, so a frozen mob still slides.
        // Going first also lets mobs that write an absolute position every frame
        // (the queen, the moth) simply overwrite it instead of flickering.
        UpdateKnockback();

        // FROZEN - everything below is the mob acting, so it is not run
        if (stunTimer > 0f)
        {
            stunTimer -= Globals.DT;

            // Blinking faster and faster over the last stretch of the freeze
            if (stunTimer <= STUN_WARNING)
            {
                float t = 1f - MathHelper.Clamp(stunTimer / STUN_WARNING, 0f, 1f);
                flickerPhase += MathHelper.Lerp(STUN_FLICKER_SLOW, STUN_FLICKER_FAST, t) * Globals.DT;
            }

            if (stunTimer <= 0f)
            {
                stunTimer = 0f;
                flickerPhase = 0f;
                thawTimer = STUN_THAW;  // wakes up slowed, not at full speed
            }

            return;
        }

        // THAWING - the mob acts, but its whole frame is shorter, so it moves,
        // aims and ticks its timers in slow motion. Scaling DT rather than a
        // speed means it works for every mob without touching one of them.
        if (thawTimer > 0f)
        {
            thawTimer = MathF.Max(0f, thawTimer - Globals.DT);

            float realDT = Globals.DT;
            Globals.DT = realDT * MathHelper.Lerp(1f, STUN_THAW_START, thawTimer / STUN_THAW);

            try
            {
                UpdateBehaviour(player, gameWorld);
            }
            finally
            {
                // Every mob after this one reads the same global - it MUST go back
                Globals.DT = realDT;
            }

            return;
        }

        UpdateBehaviour(player, gameWorld);
    }

    // What this mob does each frame: move, aim, shoot, change state
    protected abstract void UpdateBehaviour(IPlayer player, GameWorld gameWorld);

    // Longest stun wins - a second shot tops the timer up, never cuts it short
    public void Stun(float seconds)
    {
        stunTimer = MathF.Max(stunTimer, seconds);

        // Shot again mid thaw - it is frozen solid, not still waking up
        thawTimer = 0f;
        flickerPhase = 0f;
    }

    //
    // SHOVED
    //
    // Direction is the way the SHOT was flying, not away from the player.
    // REPLACED rather than added to, so emptying the rifle does not stack into
    // a launch.
    //

    // False for anything GROWN where it stands - it is attached, not standing
    protected virtual bool CanBeKnockedBack => true;

    //
    // A MOB THAT ONLY WALKS IS ONLY SHOVED SIDEWAYS
    //
    // The vertical half of the shove is dropped and what is left is normalised
    // again, so only the DIRECTION is flattened and the force is unchanged.
    // Set this on anything that walks and does not fly.
    //
    protected virtual bool HorizontalOnly => false;

    public void Knockback(Vector2 direction, float force)
    {
        if (!CanBeKnockedBack || force <= 0f || direction.LengthSquared() < 0.0001f)
            return;

        if (HorizontalOnly)
            direction = new Vector2(direction.X, 0f);

        // Straight up or down at a walker leaves nothing once flattened
        if (direction.LengthSquared() < 0.0001f)
            return;

        direction.Normalize();

        knockback = direction * force * (IsBoss ? BOSS_KNOCKBACK_SHARE : 1f);
    }

    // MoveTo rather than the sprite directly, so multi sprite mobs (the flower's
    // stem) move in one piece
    private void UpdateKnockback()
    {
        if (knockback == Vector2.Zero)
            return;

        MoveTo(Sprite.Position + knockback * Globals.DT);

        knockback *= MathF.Max(0f, 1f - KNOCKBACK_DECAY * Globals.DT);

        if (knockback.LengthSquared() < KNOCKBACK_CUTOFF * KNOCKBACK_CUTOFF)
            knockback = Vector2.Zero;
    }

    // The art as drawn, flashing red on a hit. Overridden by mobs that carry
    // their own colour.
    protected virtual Color DrawColour => Tinted(Color.White);

    // State painted over the mob's normal colour. FROZEN BEATS BLEEDING.
    // Mobs that draw themselves route their own colour through here.
    protected Color Tinted(Color body)
    {
        Color own = isHit ? Globals.Red : body;

        // Blue, except on the off half of a blink near the end of the freeze
        if (Stunned)
            return StunFlickerOff ? own : Globals.StunBlue;

        // Waking up - the blue drains out as it gets its speed back
        if (thawTimer > 0f)
            return Color.Lerp(own, Globals.StunBlue, thawTimer / STUN_THAW);

        return own;
    }

    public virtual void Draw()
    {
        Sprite.DrawRotated(SpriteRotation, DrawColour);

        if (ShowBossHP)
            GamePlayUI.DrawBossHP(HP, Sprite.Position.X + Sprite.Width / 2, Sprite.Position.Y - 20);
    }

    public virtual void TakeDamage(int damage, IAudioService audio)
    {
        MarkHit();

        if (HitSound.HasValue)
            audio.PlaySound(HitSound.Value);

        if (Invulnerable)
            return;

        HP -= damage;
    }

    public virtual void BounceFromFloor()
    {
        velocity.Y *= -3;
    }

    public virtual void Explode() {}

    // Current and max together. Bosses compare HP to MaxHP to pick a pattern.
    protected void SetHP(int hp)
    {
        HP = hp;
        MaxHP = hp;
    }

    // Later trips through the boss roster spawn tougher bosses
    public void ScaleHP(float scale)
    {
        if (scale == 1f)
            return;

        SetHP(Math.Max(1, (int)(MaxHP * scale)));
    }

    //
    // WHAT THE DIFFICULTY DID TO THIS MOB
    //
    // Applied ONCE, as the mob joins a layer (MobManager.AddMob) - the first
    // point where both "is it a boss" and "which layer" are known.
    //
    // Ordinary mob: flat multiplier. Boss: grows with the layer. See Difficulty.
    //
    public void ApplyDifficulty(int layerIndex)
    {
        bool boss = IsBoss;

        damageScale = boss ? Difficulty.BossDamage(layerIndex) : Difficulty.MobDamage;

        DAMAGE = ScaleDamage(DAMAGE);
        ScaleHP(boss ? Difficulty.BossHp(layerIndex) : Difficulty.MobHp);
    }

    // Contact damage goes through this once and is stored. Anything a mob THROWS
    // runs its own figures through here when it fires - see BatBase.Shoot.
    private float damageScale = 1f;

    protected int ScaleDamage(int damage) => Difficulty.Scale(damage, damageScale);

    public virtual void MoveTo(Vector2 position)
    {
        Sprite.Position = position;
        SyncBounds();
    }

    // MIDDLE of the mob on the point, not its top left corner. What a mob
    // spawning other mobs wants.
    public void MoveCentreTo(Vector2 centre)
    {
        MoveTo(centre - new Vector2(Sprite.Width, Sprite.Height) / 2f);
    }

    // Default spawn: anywhere across the screen, at the top of this mob's layer
    protected virtual void SetSpawn()
    {
        Sprite.Position = new Vector2(
            Globals.R.NextSingle() * (Core.windowWidth - Sprite.Width * 2) + Sprite.Width,
            LayerTopY
        );
    }

    // How much of the sprite frame counts as the mob - most frames are mostly
    // empty space. Each mob narrows this to its own art.
    protected virtual Vector2 HitboxScale => Vector2.One;

    // Outline for mobs that are nothing like a box, as fractions of the frame -
    // (0.5f, 0f) is top centre. Null means the rectangle above is enough.
    protected virtual Vector2[] HitboxShape => null;

    private Polygon hitboxPolygon;
    private bool hasPolygon = false;

    // Set only for the mobs that describe an outline
    public Polygon? HitboxPolygon => hasPolygon ? hitboxPolygon : null;

    // Keeps the bounding box on top of the sprite
    protected void SyncBounds()
    {
        Bounds = CollisionManager.UpdateBoundingRectangle(Bounds, Sprite, HitboxScale);

        if (hasPolygon)
            PlacePolygon();
    }

    // From scratch. Needed after swapping to a differently sized animation,
    // because SyncBounds only MOVES the box.
    protected void RebuildBounds()
    {
        Bounds = CollisionManager.SetBoundingRectangle(Sprite, HitboxScale);

        if (HitboxShape != null)
        {
            hitboxPolygon = Polygon.FromFractions(HitboxShape, Sprite.Width, Sprite.Height);
            hasPolygon = true;

            PlacePolygon();
        }
    }

    // Follows the drawing in both position and rotation, about the same middle
    private void PlacePolygon()
    {
        hitboxPolygon.SetPosition(Sprite.Position);
        hitboxPolygon.SetRotation(SpriteRotation, new Vector2(Sprite.Width / 2f, Sprite.Height / 2f));
    }

    // Collision goes through here, not Bounds directly, so a mob can answer with
    // its outline and the caller need not care which it is.
    //
    // Virtual because some mobs are not there to be touched (the cocoon is
    // scenery) - answering no lets bullets fly through them.
    public virtual bool CollidesWith(Rectangle rect)
    {
        if (hasPolygon)
            return hitboxPolygon.Intersects(rect);

        return Bounds.Intersects(rect);
    }

    public virtual bool CollidesWith(Circle circle)
    {
        if (hasPolygon)
            return hitboxPolygon.Intersects(circle);

        return CollisionManager.CircleIntersectsRectangle(circle, Bounds);
    }

    // Outline against outline - only the player asks this way. A mob with no
    // outline still answers correctly; SAT treats a box as a 4 sided polygon.
    public virtual bool CollidesWith(Polygon polygon)
    {
        if (hasPolygon)
            return hitboxPolygon.Intersects(polygon);

        return polygon.Intersects(Bounds);
    }

    // Call ResetDirectionTimer() AFTER reacting to it, so the random stream is
    // used in the same order every run
    protected bool DirectionTimerElapsed()
    {
        directionTimer += Globals.DT;
        return directionTimer >= directionSeconds;
    }

    protected void ResetDirectionTimer(float minSeconds = 0.5f, float maxSeconds = 5f)
    {
        directionTimer = 0f;
        directionSeconds = minSeconds + (float)Globals.R.NextDouble() * (maxSeconds - minSeconds);
    }

    protected void SetDirectionTimer(float timer, float seconds)
    {
        directionTimer = timer;
        directionSeconds = seconds;
    }

    protected void MarkHit()
    {
        isHit = true;
        hitTimer = hitDuration;
    }

    // The red flash after the mob is shot
    private void UpdateHitTimer()
    {
        if (!isHit)
            return;

        hitTimer -= Globals.DT;
        if (hitTimer <= 0f)
            isHit = false;
    }
}
