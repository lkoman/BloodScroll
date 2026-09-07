using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE SWORD - Q
//
// Not a gun and NOT in the arsenal - G and the wheel walk past it, so reaching
// for it never costs you the gun you had equipped.
//
// Cuts A THIRD OF THE CIRCLE around the player, centred on the mouse, out to
// 160px. Everything in the wedge takes the full hit, however many there are.
//
// It also CUTS MOB SHOTS out of the air (player bullets are left alone), and
// SHOVES what it hits straight out from the player. Walkers are shoved sideways
// only - see MobBase.HorizontalOnly.
//
// YOU CANNOT SHOOT MID SWING - see WeaponsManager.Spawn_bullets.
//
// ONE SWING, ONE HIT, on the frame the key went down, same as Explosion. The
// wedge is measured once; everything after that is only the picture of it, or
// the damage would depend on frame rate.
//
// 4s cooldown - the longest in the game after the stun gun.
//

public class Sword : IDrawableLayer
{
    // Over the player and over the gun, because the arm holding it is in front
    // of him - and the swing is the thing the player is looking at while it lasts
    public int DrawLayer { get; set; } = 41;

    // What the HUD calls it, and the name it is unlocked under later
    public const string NAME = "SWORD";

    // THE FIRST BOSS GIFT, not something a run starts with - see mobWaves.json
    // layer 2, and RewardService, which calls Unlock() like it hands over a gun.
    private const bool AVAILABLE_FROM_START = false;

    // How far the tip reaches from the middle of the player
    public const float REACH = 160f;

    // A THIRD OF THE CIRCLE, centred on the mouse. Must stay under half a turn
    // or the wedge stops being convex - see ArcPolygon.
    private const float ARC = MathHelper.TwoPi / 3f;
    private const float HALF_ARC = ARC / 2f;

    // 250 - the biggest single number in the game. Twice the shell's blast,
    // ten pistol shots.
    private const int DAMAGE = 250;

    private const float COOLDOWN = 4f;

    // Harder than the rifle's 800 (~100px). This clears about 200px, so the
    // wedge is EMPTY when the swing ends. See MobBase.KNOCKBACK_DECAY.
    private const float KNOCKBACK = 1400f;

    // How long the picture of the swing lasts. Short: this is a wosh, and the
    // damage was all dealt on the first frame of it anyway.
    private const float SWING_SECONDS = 0.22f;

    // The last stretch of that, spent fading out rather than turning
    private const float FADE_SECONDS = 0.08f;

    // THE TRAIL. The blade drawn several times at where it was a moment ago,
    // each fainter than the last.
    private const int GHOSTS = 6;
    private const float GHOST_STEP = 0.085f;   // how far back along the swing each one sits
    private const float GHOST_ALPHA = 0.4f;

    // How many chords the wedge's curved edge is cut into. Ten over a third of
    // a turn is a step of twelve degrees, which is smooth enough that nothing
    // can sit in the gap between a chord and the true arc.
    private const int ARC_STEPS = 10;

    public bool Unlocked { get; private set; } = AVAILABLE_FROM_START;

    // Both hands are busy. The gun reads this and holds its fire - see
    // WeaponsManager.Spawn_bullets.
    public bool Swinging => swingTimer > 0f;

    private float cooldown = 0f;
    private float swingTimer = 0f;

    // Where the mouse was when the key went down. LOCKED THERE for the whole
    // swing: the wedge was measured against that aim, so the picture has to
    // show that aim however far the mouse has moved since.
    private float aim = 0f;

    // The middle of the player, refreshed every frame - the sword is drawn from
    // it, and the swing is measured from it
    private Vector2 centre = Vector2.Zero;

    // What the last swing actually cut, kept for as long as the swing is drawn
    // so the F1 overlay can show the real wedge rather than a guess at it
    private Polygon wedge;

    // Null except during a swing - see BloodScroll.DrawDebugBoundingBoxes
    public Polygon? Arc => Swinging ? wedge : null;

    // The press, never the hold, the same guard the weapon switch uses. Holding
    // Q down would otherwise swing again the instant the four seconds were up.
    private bool canSwing = true;

    // WHAT THE HUD DRAWS. Ready is 0 the instant it is swung and climbs back to
    // 1, so the bar FILLS UP and a full bar means go - the same way round as
    // every gun's bar (see WeaponsManager.Cooldowns).
    public WeaponCooldown CooldownBar => new(
        NAME,
        MathHelper.Clamp(1f - cooldown / COOLDOWN, 0f, 1f),
        Globals.SwordSteel);

    public void Restart()
    {
        Unlocked = AVAILABLE_FROM_START;
        cooldown = 0f;
        swingTimer = 0f;
        canSwing = true;
    }

    // A sword already given is never taken away, same as a gun - see
    // WeaponsManager.UnlockNewWeapon. The boss schedule cycles forever, so the
    // gift can come round a second time and must not walk the player backwards.
    public void Unlock() => Unlocked = true;

    public void Update(IPlayer player, IAudioService audio, GameWorld gameWorld)
    {
        // The sword hangs off the middle of him, not off the gun - it is swung
        // round his body, so his body is what it turns about
        centre = player.Position + new Vector2(player.Width, player.Height) * 0.5f;

        TickTimers();

        // The clock above keeps running and a swing already begun still plays
        // itself out, but a dead player does not start another one
        if (!Globals.PLAYER_ALIVE || !Unlocked)
            return;

        ReadKey(audio, gameWorld);
    }

    private void TickTimers()
    {
        if (cooldown > 0f)
            cooldown -= Globals.DT;

        if (swingTimer > 0f)
            swingTimer -= Globals.DT;
    }

    private void ReadKey(IAudioService audio, GameWorld gameWorld)
    {
        KeyboardState keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.Q) && canSwing)
        {
            canSwing = false;

            if (cooldown <= 0f)
                Swing(audio, gameWorld);
        }
        else if (keyboardState.IsKeyUp(Keys.Q))
        {
            canSwing = true;
        }
    }

    private void Swing(IAudioService audio, GameWorld gameWorld)
    {
        aim = MathF.Atan2(
            Globals.MousePosition.Y - centre.Y,
            Globals.MousePosition.X - centre.X);

        wedge = BuildWedge(centre, aim);

        Strike(audio, gameWorld);

        audio.PlaySound(AudioId.SwordSwing);

        swingTimer = SWING_SECONDS;
        cooldown = COOLDOWN;
    }

    //
    // THE WEDGE, AS A POLYGON
    //
    // A fan: the middle of the player, then the rim walked from one edge of the
    // arc to the other. A sector up to HALF A TURN is convex, which is what SAT
    // needs (see Polygon) - opening the arc past a half BREAKS the hit test
    // rather than merely widening it.
    //
    private static Polygon BuildWedge(Vector2 centre, float angle)
    {
        var vertices = new Vector2[ARC_STEPS + 2];

        vertices[0] = centre;

        // Straight chords cut INSIDE the curve, so the middle of each falls
        // short of the reach. Pushing the corners out by that much puts every
        // chord's halfway point back on the real radius.
        float radius = REACH / MathF.Cos(ARC / (2f * ARC_STEPS));

        for (int i = 0; i <= ARC_STEPS; i++)
        {
            float a = angle - HALF_ARC + ARC * i / ARC_STEPS;

            vertices[i + 1] = centre + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
        }

        return new Polygon(vertices);
    }

    // EVERYTHING IN THE WEDGE, ONCE. Every layer still being simulated, not
    // just the player's - a bat can follow him up through the ceiling.
    private void Strike(IAudioService audio, GameWorld gameWorld)
    {
        foreach (var layer in gameWorld.ActiveLayers())
        {
            foreach (var mob in layer.MobManager.mobs)
            {
                if (!mob.CollidesWith(wedge))
                    continue;

                mob.TakeDamage(DAMAGE, audio);

                // STRAIGHT OUT FROM THE PLAYER, unlike a bullet's knockback,
                // which goes the way the shot was flying.
                //
                // The mob has the last word: walkers flatten it to left/right,
                // rooted mobs refuse it. Same path as the rifle's knockback.
                mob.Knockback(mob.Bounds.Center.ToVector2() - centre, KNOCKBACK);
            }
        }

        CutShots(gameWorld);
    }

    //
    // THE SHOTS IN THE WEDGE GO WITH THEM
    //
    // Only what is ALREADY IN the arc when the blade comes round - the swing is
    // not a shield. A shot that arrives a frame later arrives.
    // The player's own bullets are left alone.
    //
    private void CutShots(GameWorld gameWorld)
    {
        foreach (Bullet shot in gameWorld.MobProjectiles)
        {
            if (shot.active == 0 || !wedge.Intersects(shot.bulletBounds))
                continue;

            shot.active = 0;
        }
    }

    public void Draw()
    {
        if (swingTimer <= 0f)
            return;

        // 0 the frame it was swung, 1 as it finishes
        float t = 1f - swingTimer / SWING_SECONDS;

        // Fades out over the last few hundredths, so the blade is not simply
        // switched off at the end of the arc
        float alpha = MathHelper.Clamp(swingTimer / FADE_SECONDS, 0f, 1f);

        // FAINTEST FIRST, so the solid blade is laid over its own trail rather
        // than under it
        for (int i = GHOSTS; i >= 1; i--)
        {
            float ghost = t - i * GHOST_STEP;

            if (ghost < 0f)
                continue;

            // Thinning out the further back down the swing it sits
            float fade = GHOST_ALPHA * (1f - (float)i / (GHOSTS + 1)) * alpha;

            DrawBlade(AngleAt(ghost), Globals.SwordSteel * fade);
        }

        DrawBlade(AngleAt(t), Color.White * alpha);
    }

    // From one edge of the wedge to the other, passing through the mouse
    // halfway. EASED OUT - leaves fast, arrives slow.
    private float AngleAt(float t)
    {
        t = MathHelper.Clamp(t, 0f, 1f);

        float eased = 1f - (1f - t) * (1f - t);

        return aim - HALF_ARC + ARC * eased;
    }

    // Turned about its own pommel, which is sat on the middle of the player -
    // the same origin the gun is drawn from, for the same reason
    private void DrawBlade(float angle, Color colour)
    {
        Texture2D blade = SwordTexture.Get((int)REACH);

        Globals.SpriteBatch.Draw(
            blade,
            centre,
            null,
            colour,
            angle,
            new Vector2(0f, blade.Height / 2f),
            1f,
            SpriteEffects.None,
            0f
        );
    }
}
