using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// AMBUSH MOB - THE FLOWER
//
// Sits asleep on a platform. Walk near it and it wakes; walk onto its ledge or
// the one next to it and the head is thrown at you on the stalk and bites.
// Then it is pulled back in and has to wind up again.
//
// Placed when the layer is built, not in a wave - it needs a platform.
//
// Two ways to deal with one: shoot it from a ledge away, or land on its
// platform and PLUCK it with E, which gives you a bomb to carry.
//
// HOW IT IS PUT TOGETHER (omejevanje)
//
// Two drawings held together by a constraint, plus a third repeated along it:
//
//   LEAVES   fixed anchor on the platform. Never moves.
//   HEAD     one particle tied to the anchor by a damped spring
//            (MonoGameLibrary/Spring.cs). Everything is done by steering that
//            spring, never by setting the head's position.
//   STEM     not a sprite. TiledRope repeats one short piece from the leaves
//            to the head, so the stalk grows as the spring stretches.
//
// THE HEAD IS THE MOB - MobBase.Sprite is the head, so bullets hit the head,
// the hitbox follows the head, and the bite lands when the HEAD touches.
//
// The whole behaviour is steering the spring:
//   idle     short rest length, pointing up, soft, alternating sideways push
//   coil     rest length pulled in BELOW resting - the compressed wind up
//   strike   rest point thrown onto the player, stiffness multiplied
//   recover  rest point back to idle, damping raised, head reels itself in
//

public class Flower : MobBase, IPlatformMob
{
    private enum FlowerState { Sleeping, Waking, Idle, Coiling, Striking, Recovering }

    private FlowerState state = FlowerState.Sleeping;

    private AnimatedSprite _sleeping, _idle, _bite, _leaves;
    private TiledRope _stem;

    // THE CONSTRAINT. Built in PlaceOnPlatform, once we know where the root is.
    private Spring _spring;

    //
    // WHICH WAY THE HEAD IS DRAWN
    //
    // The head turns to look along the stalk, so the code needs to know where
    // the face points BEFORE any rotation:
    //   UP -> -PiOver2,  RIGHT -> 0f,  DOWN -> PiOver2
    // Draw the head CENTRED in its frame - that is the join and the pivot.
    //
    private const float ART_FACING = -MathHelper.PiOver2;

    // The art is small - a 67px head next to a 124px player on a 128px platform -
    // so it is scaled up. 1f draws everything at the size it was drawn.
    private const float ART_SCALE = 1.5f;

    // How much each stem piece overlaps the one before it. The drawing is 4x8
    // of solid stalk in a 12px region (2px bleed top and bottom), so spacing by
    // region height alone leaves gaps. Above 0.33 closes them.
    private const float STEM_OVERLAP = 0.45f;

    //
    // SHAPE
    //

    //
    // ONE CIRCLE FOR EVERYTHING
    //
    // What it can SEE and what it can HIT are the same distance: `reach`, worked
    // out from the ledges around it in PlaceOnPlatform.
    //
    // It is also the hard limit on the constraint, so the stalk cannot carry the
    // head outside it however hard the spring is driven.
    //
    private float reach = 100f;

    // How high the head floats while idling, in PLAYERS - read off the player
    // sprite, not pixels. NOT a fraction of the reach.
    private const float IDLE_HEIGHT_IN_PLAYERS = 1.05f;
    private static float IdleLength => Player.PLAYER_HEIGHT * IDLE_HEIGHT_IN_PLAYERS;

    // Share of the idle height it is pulled in to while winding up. SHORTER than
    // idle, so the spring is genuinely compressed.
    private const float COIL_SHARE = 0.45f;
    private static float CoilLength => IdleLength * COIL_SHARE;

    // Asleep: half the bud, so it sits ON the platform. Taken from the drawing,
    // so a redrawn bud of any height still sits flat.
    private float SleepLength => _sleeping.Height * 0.5f;

    //
    // THE SPRING'S SETTINGS PER STATE
    //

    // Soft and barely damped, so the sway carries on
    private const float IDLE_STIFFNESS = 55f;
    private const float IDLE_DAMPING = 2.2f;

    // Firmer while coiling - it has to hold itself compressed, not sag
    private const float COIL_STIFFNESS = 90f;
    private const float COIL_DAMPING = 7f;

    // The snap. Stiff and almost undamped is what makes it whip out.
    private const float STRIKE_STIFFNESS = 700f;
    private const float STRIKE_DAMPING = 3.5f;

    // Heavily damped coming back, so it settles instead of pinging about
    private const float RECOVER_STIFFNESS = 120f;
    private const float RECOVER_DAMPING = 14f;

    // THE IDLE SWAY. A push left, then right, at the rate the spring already
    // swings at. The bounce is the spring's, not an animation's.
    private const float SWAY_FORCE = 900f;
    private const float SWAY_SPEED = 2.6f; // radians a second
    private float swayPhase = 0f;

    //
    // TIMING
    //
    private const float WAKE_SECONDS = 0.45f;
    private const float COIL_SECONDS = 0.40f;  // the telegraph
    private const float STRIKE_SECONDS = 0.35f; // how long the bite stays out
    private const float RECOVER_SECONDS = 0.9f;
    private const float REST_SECONDS = 0.7f;   // breather between bites
    private float stateTimer = 0f;

    //
    // HOW THE CIRCLE IS SIZED
    //
    // It must reach the MIDDLE of the neighbouring ledges. Gaps are sized by
    // how far the player can jump and differ every layer, so this is MEASURED
    // off the real neighbours rather than typed in.
    //
    // Floor is its own ledge plus this much past either end.
    private const float WAKE_MARGIN = 50f;

    // How many ledges count as "surrounding" - the one before and the one after
    private const int NEIGHBOUR_LEDGES = 2;

    // Hard cap, so one oddly placed platform cannot make a screen-wide flower
    private const float REACH_CAP = 520f;

    // The ground slab is a platform too and its middle is half a screen away.
    // Anything this wide is scenery, not a ledge.
    private const float MAX_LEDGE_WIDTH = 500f;

    // Where the leaves sit: the fixed end of the constraint, on the platform
    private Vector2 root;

    // The strip of air just above its platform - standing here wakes it
    private Rectangle trigger;

    // PLUCKING. Standing on THAT ledge - not near it, not the one next door.
    private Platform ledge;

    // How far past either end still counts as standing on it, so the prompt does
    // not blink out when the player is half off the edge
    private const float LEDGE_MARGIN = 24f;

    // How far his feet may be off the top - he settles onto a platform rather
    // than landing exactly on it
    private const float FOOT_SLACK = 12f;

    protected override AudioId? HitSound => AudioId.BatSqueak;

    // Only counts once woken - a flower the player never touched must not stop
    // him finishing the layer
    public override bool CountsAsEnemy => state != FlowerState.Sleeping;

    // The head is mostly petals, and the bite is the middle of it
    protected override Vector2 HitboxScale => new(0.70f, 0.70f);

    public Flower()
    {
        // Tough, so that plucking it stays worth the risk of standing on its
        // ledge
        SetHP(200);
        DAMAGE = 40;
        PointsOnKill = 75;
        ON_TOUCH = OnTouch.Nothing; // only the bite itself hurts
    }

    public override void LoadContent(Vector2 _, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        _sleeping = MobArt.Flower(MobArt.FlowerSleeping);
        _idle = MobArt.Flower(MobArt.FlowerIdle);
        _bite = MobArt.Flower(MobArt.FlowerBite);
        _leaves = MobArt.Flower(MobArt.FlowerLeaves);

        _stem = new TiledRope(MobArt.Flower(MobArt.FlowerStem))
        {
            Scale = ArtScale,
            Overlap = STEM_OVERLAP,
        };

        _sleeping.Scale = ArtScale;
        _idle.Scale = ArtScale;
        _bite.Scale = ArtScale;
        _leaves.Scale = ArtScale;

        Sprite = _sleeping;

        RebuildBounds();
    }

    private static Vector2 ArtScale => new(ART_SCALE, ART_SCALE);

    // Platform positions are in world space, up is negative
    public void PlaceOnPlatform(Platform platform, IReadOnlyList<Platform> layerPlatforms)
    {
        ledge = platform;

        // The root is the middle of the top edge of the platform. The leaves
        // are drawn standing on it and never move again.
        root = new Vector2(
            platform.position.X + platform.Width / 2f,
            platform.position.Y
        );

        _leaves.Position = root - new Vector2(_leaves.Width / 2f, _leaves.Height);

        reach = MeasureReach(platform, layerPlatforms);

        // THE CONSTRAINT, pinned to that root. Asleep it is collapsed down to
        // nothing: the head rests among the leaves and no stalk shows at all.
        _spring = new Spring(root)
        {
            RestLength = SleepLength,
            RestDirection = -Vector2.UnitY,
            MaxLength = reach,
            Stiffness = IDLE_STIFFNESS,
            Damping = IDLE_DAMPING,
        };

        _spring.SnapTo(root - new Vector2(0f, SleepLength));

        // A little wider than the platform so walking on from either end counts
        trigger = new Rectangle(
            (int)platform.position.X - 16,
            (int)platform.position.Y - (int)Sprite.Height,
            (int)platform.Width + 32,
            (int)Sprite.Height + (int)platform.Height
        );

        PlaceHead();
        RebuildBounds();
    }

    // Walks out to the nearest few neighbours and takes the furthest, so the
    // stalk is only as long as the jump it has to cover.
    //
    // Measured to the TOP MIDDLE of each neighbour - where the player actually
    // stands, not the middle of the platform sprite.
    private float MeasureReach(Platform own, IReadOnlyList<Platform> layerPlatforms)
    {
        float measured = own.Width / 2f + WAKE_MARGIN;

        if (layerPlatforms == null)
            return measured;

        var neighbours = layerPlatforms
            .Where(p => p != own && p.Width <= MAX_LEDGE_WIDTH)
            .Select(p => Vector2.Distance(root, new Vector2(p.position.X + p.Width / 2f, p.position.Y)))
            .OrderBy(distance => distance)
            .Take(NEIGHBOUR_LEDGES);

        foreach (float distance in neighbours)
            measured = MathF.Max(measured, distance);

        return MathF.Min(measured, REACH_CAP);
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        // Only if it was never placed on a platform, which cannot happen
        // through MobManager but keeps a hand-spawned flower from crashing
        if (_spring == null)
            return;

        // BEFORE ANYTHING ELSE - a plucked flower has no behaviour left to run
        if (OfferPluck(player))
            return;

        stateTimer += Globals.DT;

        switch (state)
        {
            case FlowerState.Sleeping:  Sleeping(player); break;
            case FlowerState.Waking:    Waking(); break;
            case FlowerState.Idle:      Idle(player); break;
            case FlowerState.Coiling:   Coiling(player); break;
            case FlowerState.Striking:  Striking(); break;
            case FlowerState.Recovering:Recovering(); break;
        }

        // THE CONSTRAINT RUNS LAST. Everything above only changed what the spring
        // pulls TOWARDS; this is the line that decides where the head ends up.
        _spring.Update(Globals.DT);

        PlaceHead();

        Sprite.Update();
        _leaves.Update();
        _stem.Update();
    }

    //
    // PLUCKING
    //

    // Shows the prompt while the player stands on this flower's ledge and takes
    // the flower off if he presses the key. True only on the frame it was
    // plucked. A player already carrying a bomb is never offered a second.
    private bool OfferPluck(IPlayer player)
    {
        if (player.HasBomb || !StandingOnMyLedge(player))
            return false;

        // The HUD writes it along the bottom - the flower only says the offer
        // is on, not where the words go
        GamePlayUI.OfferPluck();

        if (!player.TryTakeInteract())
            return false;

        // Dies the same way it would to a bullet, so the layer, the score and the
        // mob list all handle it as a kill
        HP = 0;

        player.GiveBomb();

        return true;
    }

    // On THIS ledge: feet on its top edge, somewhere between its ends. Being in
    // the AIR over it does not count.
    private bool StandingOnMyLedge(IPlayer player)
    {
        if (ledge == null || player.InAir)
            return false;

        float feet = player.Position.Y + player.Height;

        if (feet < ledge.position.Y - FOOT_SLACK || feet > ledge.position.Y + ledge.Height)
            return false;

        float middle = player.Position.X + player.Width / 2f;

        return middle >= ledge.position.X - LEDGE_MARGIN &&
               middle <= ledge.position.X + ledge.Width + LEDGE_MARGIN;
    }

    //
    // STATES
    //

    private void Sleeping(IPlayer player)
    {
        if (!player.HurtBox.Intersects(trigger) && !WithinOf(player, reach))
            return;

        EnterState(FlowerState.Waking);

        // Invulnerable while coming up, so it always gets one bite off
        Invulnerable = true;
        SwapHead(_idle);
    }

    private void Waking()
    {
        // The rest length is walked from nothing to the idle height and the
        // spring drags the head after it, overshooting because it is barely
        // damped. THAT OVERSHOOT IS THE FLOWER OPENING - nothing animates it.
        float t = MathF.Min(stateTimer / WAKE_SECONDS, 1f);
        _spring.RestLength = MathHelper.Lerp(SleepLength, IdleLength, t);

        if (stateTimer < WAKE_SECONDS)
            return;

        Invulnerable = false;
        EnterState(FlowerState.Idle);
    }

    private void Idle(IPlayer player)
    {
        _spring.Stiffness = IDLE_STIFFNESS;
        _spring.Damping = IDLE_DAMPING;
        _spring.RestDirection = -Vector2.UnitY;
        _spring.RestLength = IdleLength;

        Sway();

        // Waits out its breather before it will look at the player again
        if (stateTimer < REST_SECONDS)
            return;

        if (WithinOf(player, reach))
            EnterState(FlowerState.Coiling);
    }

    // Pushes the head sideways, one way then the other. A force, not a
    // position - the spring turns it into a swing and the stalk bends with it
    // because the stem is drawn along whatever line the spring ends up on.
    private void Sway()
    {
        swayPhase += SWAY_SPEED * Globals.DT;
        _spring.ApplyForce(new Vector2(MathF.Sin(swayPhase) * SWAY_FORCE, 0f));
    }

    private void Coiling(IPlayer player)
    {
        // Pulled in short and leaned AWAY from the player. The spring is now
        // compressed and aimed backwards, which is what gives the strike its
        // snap - and it is a whole third of a second of the flower visibly
        // rearing back before anything can hurt you.
        _spring.Stiffness = COIL_STIFFNESS;
        _spring.Damping = COIL_DAMPING;
        _spring.RestLength = CoilLength;

        Vector2 away = root - MiddleOf(player);
        away.Y -= 40f; // still leaning upwards, never down into the platform
        _spring.RestDirection = away;

        if (stateTimer < COIL_SECONDS)
            return;

        // COMMITTED. The bite goes where the player was standing when the
        // wind up finished, not wherever he has got to since - so it can be
        // read and stepped out of, same as the spider queen's ram.
        _spring.Stiffness = STRIKE_STIFFNESS;
        _spring.Damping = STRIKE_DAMPING;
        _spring.ReachFor(MiddleOf(player));

        ON_TOUCH = OnTouch.HurtPlayer;

        EnterState(FlowerState.Striking);
        SwapHead(_bite);
    }

    private void Striking()
    {
        if (stateTimer < STRIKE_SECONDS)
            return;

        // Bite over. Let go of the aim and the spring hauls it back by itself.
        _spring.Stiffness = RECOVER_STIFFNESS;
        _spring.Damping = RECOVER_DAMPING;
        _spring.RestDirection = -Vector2.UnitY;
        _spring.RestLength = IdleLength;

        ON_TOUCH = OnTouch.Nothing;

        EnterState(FlowerState.Recovering);
        SwapHead(_idle);
    }

    private void Recovering()
    {
        // Back to idling once it is home, or once it has had long enough -
        // a head wedged at full stretch must not lock the flower up
        bool settled = _spring.StretchFromRest < 12f;

        if (settled || stateTimer >= RECOVER_SECONDS)
            EnterState(FlowerState.Idle);
    }

    private void EnterState(FlowerState next)
    {
        state = next;
        stateTimer = 0f;
    }

    //
    // KEEPING THE MOB ON TOP OF THE SPRING
    //

    // The head sprite is hung on the moving end of the constraint: the spring
    // decides where it is, this only copies it across. Position is the top
    // left corner everywhere in the game, so the particle - which is the
    // MIDDLE of the head - is offset by half a frame.
    private void PlaceHead()
    {
        Sprite.Position = _spring.Position - new Vector2(Sprite.Width, Sprite.Height) * 0.5f;

        // Looks along its own stalk, so it leans into the sway and points
        // straight down the line of the bite
        Vector2 direction = _spring.Direction;
        SpriteRotation = MathF.Atan2(direction.Y, direction.X) - ART_FACING;

        SyncBounds();
    }

    private void SwapHead(AnimatedSprite next)
    {
        if (Sprite == next)
            return;

        next.Position = Sprite.Position;
        Sprite = next;

        RebuildBounds();
    }

    private bool WithinOf(IPlayer player, float radius)
    {
        return Vector2.DistanceSquared(root, MiddleOf(player)) <= radius * radius;
    }

    private static Vector2 MiddleOf(IPlayer player) =>
        player.Position + new Vector2(player.Width, player.Height) * 0.5f;

    // DRAWING, bottom up: leaves, stalk, then the head over the join.

    public override void Draw()
    {
        if (_spring == null)
            return;

        Color tint = DrawColour;

        // ASLEEP IT IS ONE DRAWING AND NOTHING ELSE - no leaves, no stalk,
        // just the closed bud sitting on the platform. The whole plant only
        // unfolds once it has been disturbed, which is what makes it read as
        // a harmless thing growing on a ledge right up until it isn't.
        if (state != FlowerState.Sleeping)
        {
            _leaves.Draw(tint);
            _stem.Draw(root, _spring.Position, tint);
        }

        base.Draw();
    }

    // Rooted to its platform - it does not fall, and it does not get bounced
    public override void BounceFromFloor() {}

    // Nor shoved. A rifle round into a plant that is GROWING out of the ledge
    // does not push the plant back, and the head swinging on its stem is
    // already the whole of how a flower reacts to being hit.
    protected override bool CanBeKnockedBack => false;

    // Nothing spawns a flower loose in the air, but if something ever does,
    // the whole plant moves: root, leaves, constraint and head together
    public override void MoveTo(Vector2 position)
    {
        if (_spring == null)
        {
            base.MoveTo(position);
            return;
        }

        Vector2 delta = position - root;

        root += delta;
        _leaves.Position += delta;
        _spring.MoveAnchor(root);

        PlaceHead();
    }
}
