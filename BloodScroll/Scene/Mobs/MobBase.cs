using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// SHARED BASE FOR EVERY MOB
//
// Holds the parts each mob used to repeat by hand: the red hit-flash timer,
// the random direction timer, spawning, the floor bounce and the standard draw.
// A subclass only has to write LoadContent + UpdateBehaviour, and override the
// rest where it actually differs.
//
// Coordinate note: up is NEGATIVE. A mob on layer N lives around
// y = -windowHeight * N, which is what LayerTopY gives you.
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

    // The sprite the base class draws and builds the bounding box from.
    // Mobs that swap between animations reassign this as they change state.
    protected AnimatedSprite Sprite;

    protected int SpawnLayer;
    protected Vector2 velocity;
    protected float speed, max_speed;

    // How far the sprite is turned, in radians, about the middle of its frame.
    // Almost every mob leaves this alone and flips left or right instead; the
    // ones that aim their whole body at the player drive it, and their outline
    // hitbox turns with the drawing.
    protected float SpriteRotation = 0f;

    // An invulnerable mob still flashes and makes a noise when shot,
    // it just does not lose HP (the moth while it hides in its cocoon).
    protected bool Invulnerable = false;

    // A BOSS IS THE MOB THAT DRAWS ITS HP OVER ITS HEAD.
    //
    // The two used to be separate and could drift apart; they are the same
    // question asked twice. If the player can see its health bar, it is the
    // thing the room is about - so it is also the thing whose death ends the
    // room, and the thing whose HP grows each time it comes round again.
    public virtual bool IsBoss => ShowBossHP;

    // Bosses draw their remaining HP above their head
    protected virtual bool ShowBossHP => false;

    // Has to die before the layer is cleared. Overridden by the mobs that
    // are pickups or ambushes rather than enemies you are meant to hunt down.
    public virtual bool CountsAsEnemy => true;

    // Bullets stop in almost everything. The two that are set off by touching
    // them rather than by shooting them say no here, and shots pass through.
    public virtual bool StopsBullets => true;

    // And those same two are not kills the player made, so they do not feed
    // the life steal gift.
    public virtual bool GivesLifeSteal => true;

    // Sound played when this mob is shot. Null means silent.
    protected virtual AudioId? HitSound => AudioId.BatSqueak;

    // HIT TIMER
    protected bool isHit = false;
    private float hitTimer = 0f;
    private const float hitDuration = 0.15f; // seconds

    // STUN TIMER
    // A stunned mob is skipped entirely: it does not move, does not aim, does
    // not tick its own state machine on. It still has a hitbox and it still
    // hurts to walk into - it is frozen, not switched off.
    private float stunTimer = 0f;
    protected bool Stunned => stunTimer > 0f;

    // KNOCKBACK
    //
    // A shove the mob did not ask for, in pixels per second, bled off a little
    // more every frame. Only the rifle deals any - see WeaponSpec.Knockback.
    //
    // It is a DISPLACEMENT laid over whatever the mob was doing rather than a
    // change to its own velocity, because almost every mob steers itself and
    // would simply steer the push out again. Shoved back, then walking back in,
    // is exactly what the gun is for.
    private Vector2 knockback = Vector2.Zero;

    // How fast the shove dies. The mob travels roughly force/decay pixels in
    // total, so at 7 a hit of 800 moves it a little over a hundred - a real
    // step backwards, not a teleport.
    private const float KNOCKBACK_DECAY = 7f;

    // Below this it is a pixel a second and not worth the arithmetic
    private const float KNOCKBACK_CUTOFF = 4f;

    // A BOSS IS HEAVY. It takes the same shove as everything else and moves a
    // third as far - enough to see the hit land, never enough to walk the thing
    // the room is about into a corner and hold it there.
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

        // BEFORE THE MOB MOVES, and before the stun check, because being shoved
        // is not the mob acting - a frozen mob still slides when it is hit.
        // Going first also means the handful of mobs that write an absolute
        // position every frame (the queen on her track, the moth on its perch)
        // simply overwrite it and stand their ground, instead of flickering
        // between shoved and snapped back.
        UpdateKnockback();

        // FROZEN. Everything below this line is the mob acting, and a stunned
        // mob does not act - so the whole behaviour is simply not run.
        if (stunTimer > 0f)
        {
            stunTimer -= Globals.DT;
            return;
        }

        UpdateBehaviour(player, gameWorld);
    }

    // Everything this particular mob does each frame: move, aim, shoot, change state.
    protected abstract void UpdateBehaviour(IPlayer player, GameWorld gameWorld);

    // The longest stun on the mob wins, so a second shot on something already
    // frozen tops the timer up instead of cutting it short.
    public void Stun(float seconds)
    {
        stunTimer = MathF.Max(stunTimer, seconds);
    }

    //
    // SHOVED
    //
    // The direction is the way the shot was flying, so the mob always goes the
    // way the bullet was going rather than away from the player - shooting a bat
    // that has got above you knocks it up, not sideways.
    //
    // REPLACED rather than added to, the same way the player's own knockback is:
    // emptying the rifle into one thing pushes it steadily, it does not stack
    // into a launch.
    //
    // Whether a shove moves this mob at all. Anything GROWN where it stands
    // says no - it is not standing there, it is attached there.
    protected virtual bool CanBeKnockedBack => true;

    public void Knockback(Vector2 direction, float force)
    {
        if (!CanBeKnockedBack || force <= 0f || direction.LengthSquared() < 0.0001f)
            return;

        direction.Normalize();

        knockback = direction * force * (IsBoss ? BOSS_KNOCKBACK_SHARE : 1f);
    }

    // Slides the mob along what is left of the shove and takes some of it away.
    // MoveTo rather than touching the sprite, so the mobs that are more than one
    // sprite (the flower's whole stem) move in one piece.
    private void UpdateKnockback()
    {
        if (knockback == Vector2.Zero)
            return;

        MoveTo(Sprite.Position + knockback * Globals.DT);

        knockback *= MathF.Max(0f, 1f - KNOCKBACK_DECAY * Globals.DT);

        if (knockback.LengthSquared() < KNOCKBACK_CUTOFF * KNOCKBACK_CUTOFF)
            knockback = Vector2.Zero;
    }

    // What the sprite is drawn in: the art as it was drawn, flashing red while
    // the mob is taking a hit. Overridden by the mobs that carry their own
    // colour instead of art, where the flash has to be something else to show.
    protected virtual Color DrawColour => Tinted(Color.White);

    // The state a mob is in, painted over whatever colour it normally wears.
    // Frozen beats bleeding: a stunned mob has to stay readable as stunned even
    // while the player is unloading into it.
    //
    // Mobs that draw themselves (the jellyfish, the butterfly, the flower)
    // route their own colour through here rather than repeating the order.
    protected Color Tinted(Color body)
    {
        if (Stunned)
            return Globals.StunBlue;

        return isHit ? Globals.Red : body;
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

    // Sets current and maximum HP together. Bosses compare HP against MaxHP
    // to decide which attack pattern they are on.
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
    // Applied ONCE, as the mob joins a layer (see MobManager.AddMob) - which is
    // the first moment both halves of the question can be answered: whether it
    // is a boss, and which layer it is standing on. Neither is known in the
    // constructor, where the raw HP and damage are written.
    //
    // An ordinary mob takes a flat multiplier and the layer never enters into
    // it. A boss takes one that grows with the climb. That difference is the
    // whole shape of the curve - see Difficulty.
    //
    public void ApplyDifficulty(int layerIndex)
    {
        bool boss = IsBoss;

        damageScale = boss ? Difficulty.BossDamage(layerIndex) : Difficulty.MobDamage;

        DAMAGE = ScaleDamage(DAMAGE);
        ScaleHP(boss ? Difficulty.BossHp(layerIndex) : Difficulty.MobHp);
    }

    // What this mob's damage is actually worth. Contact damage above is put
    // through it once and stored; everything a mob THROWS is worked out while
    // it is alive, so each shooter runs its own figures through here at the
    // moment it fires - see BatBase.Shoot for the pattern.
    private float damageScale = 1f;

    protected int ScaleDamage(int damage) => Difficulty.Scale(damage, damageScale);

    public virtual void MoveTo(Vector2 position)
    {
        Sprite.Position = position;
        SyncBounds();
    }

    // Puts the MIDDLE of the mob on the point instead of its top left corner.
    // What a mob handing out other mobs wants: the hole spits its adds out of
    // its centre, not out of the corner of the frame around it.
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

    // How much of the sprite frame actually counts as the mob. Most frames are
    // mostly empty space, and using the whole thing is what makes hits feel
    // cheap. Each mob narrows this to fit its own artwork.
    protected virtual Vector2 HitboxScale => Vector2.One;

    // An outline for mobs whose shape is nothing like a box, given as
    // fractions of the sprite frame - (0.5f, 0f) is top centre. Null means
    // the rectangle above is good enough, which it is for most mobs.
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

    // Rebuilds the box from scratch. Needed after swapping to an animation
    // of a different size, because SyncBounds only moves the box.
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

    // The outline follows the drawing on both counts: where it is and which
    // way it is turned, about the same middle of the frame the sprite turns on
    private void PlacePolygon()
    {
        hitboxPolygon.SetPosition(Sprite.Position);
        hitboxPolygon.SetRotation(SpriteRotation, new Vector2(Sprite.Width / 2f, Sprite.Height / 2f));
    }

    // Collision goes through here rather than reading Bounds directly, so a mob
    // can answer with its outline instead of its box without the collision code
    // needing to care which it is.
    //
    // Virtual because a couple of mobs are not there to be touched at all: the
    // cocoon is scenery, and the moth inside it is not in the arena. Answering
    // no here is what lets bullets fly straight through them.
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

    // Outline against outline - the player is the one thing that asks this
    // way. A mob without an outline of its own still answers honestly: SAT
    // treats a box as the four sided polygon it is.
    public virtual bool CollidesWith(Polygon polygon)
    {
        if (hasPolygon)
            return hitboxPolygon.Intersects(polygon);

        return polygon.Intersects(Bounds);
    }

    // True once the direction timer has run out. Call ResetDirectionTimer()
    // *after* reacting to it, so the random stream is used in the same order.
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

    // Drives the red flash after the mob is shot
    private void UpdateHitTimer()
    {
        if (!isHit)
            return;

        hitTimer -= Globals.DT;
        if (hitTimer <= 0f)
            isHit = false;
    }
}
