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
// the one next to it and the head is thrown at you on the end of its stalk and
// bites. Then it is pulled back in and has to wind up again.
//
// Placed when the layer is built rather than in a wave, because it needs a
// platform to sit on.
//
// TWO WAYS TO DEAL WITH ONE
//
// Shoot it from a ledge away, which is safe and slow - it has a lot of HP and
// you are standing still while you spend it. Or land ON its platform and PLUCK
// it with E, which is instant, gives you a bomb to carry, and puts you inside
// its reach with the wind up already running. The prompt only shows while you
// are standing there, because that is the only place the choice exists.
//
//
// HOW IT IS PUT TOGETHER (omejevanje)
//
// The flower is not one sprite. It is two drawings held together by a
// constraint, and a third drawing repeated along that constraint:
//
//   LEAVES   fixed, sitting on the platform. This is the anchor - the wall
//            the pendulum is nailed to. It never moves.
//   HEAD     a single particle, tied to that anchor by a damped spring
//            (MonoGameLibrary/Spring.cs). Everything the flower does is done
//            by steering that spring, never by setting the head's position.
//   STEM     never drawn as a sprite of its own. TiledRope repeats one short
//            piece of stem from the leaves to the head, so the stalk grows a
//            piece at a time as the spring stretches - the connection is what
//            you see, but what is really there is the spring.
//
// The head is the mob as far as the rest of the game is concerned: MobBase's
// Sprite is the head, so bullets hit the head, the hitbox follows the head,
// and the bite lands when the HEAD touches the player, not the leaves.
//
// Steering the spring is three numbers and that is the whole behaviour:
//   idle    short rest length, pointing up, soft and barely damped, plus a
//           sideways push that alternates - it sways
//   coil    rest length pulled in BELOW the resting one - the spring is
//           compressed, which is the wind up the player gets to read
//   strike  rest point thrown onto the player and the stiffness multiplied -
//           the spring is now badly out of shape and snaps the head out there
//   recover rest point handed back to idle, damping raised, and the spring
//           reels the head in on its own
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
    // The head is turned to look along the stalk, so the code has to know
    // where the face points in the drawing BEFORE anything is turned:
    //   drawn pointing UP    -> -MathHelper.PiOver2
    //   drawn pointing RIGHT ->  0f
    //   drawn pointing DOWN  ->  MathHelper.PiOver2
    // Draw the head centred in its frame - the middle of the frame is the
    // point the stalk is joined to and the point it spins on.
    //
    private const float ART_FACING = -MathHelper.PiOver2;

    // The drawings are small - a 67px head next to a 124px player, on a 128px
    // platform - so they are blown up to sit properly on a ledge. At 1.5 the
    // leaves cover most of the platform and the head is about two thirds of
    // the player. 1f draws everything at the size it was drawn.
    private const float ART_SCALE = 1.5f;

    // How much of itself each stem piece is laid back over the one before it.
    // The stem drawing is 4x8 of solid stalk inside a 12px tall region - 2px
    // of transparent bleed top and bottom, and rounded caps on the ends - so
    // pieces laid out by the region height alone would leave gaps between the
    // solid parts. Anything above 0.33 closes them. Raise it if you redraw the
    // piece with more padding, lower it if the stalk starts looking bunched.
    private const float STEM_OVERLAP = 0.45f;

    //
    // SHAPE
    //

    //
    // ONE CIRCLE FOR EVERYTHING
    //
    // What it can see and what it can hit are the SAME distance: `reach`,
    // worked out from the ledges around it in PlaceOnPlatform. It cannot
    // notice a player it could not bite, and it cannot bite past where it
    // notices - so a flower that has woken up is always a flower already in
    // range, and stepping out of the circle is what ends the fight.
    //
    // It is also the hard limit on the constraint, so the stalk physically
    // cannot carry the head outside it however hard the spring is driven.
    //
    private float reach = 100f;

    // How high the head floats while it is awake and idling, in players.
    // Read off the player sprite rather than typed in pixels, so it stays
    // "about head height on someone standing next to it" whatever happens to
    // the art. This is deliberately NOT a fraction of the reach - a flower
    // that could bite two ledges away should not idle two ledges up.
    private const float IDLE_HEIGHT_IN_PLAYERS = 1.05f;
    private static float IdleLength => Player.PLAYER_HEIGHT * IDLE_HEIGHT_IN_PLAYERS;

    // Pulled in to this share of the idle height while it winds up. Shorter
    // than idle, so the spring is genuinely compressed and the wind up reads
    // as a crouch rather than as the flower just stopping.
    private const float COIL_SHARE = 0.45f;
    private static float CoilLength => IdleLength * COIL_SHARE;

    // And while it is asleep: half the bud, so the closed bud is sitting ON
    // the platform rather than floating over it. Taken from the drawing, so a
    // redrawn bud of any height still sits flat on the ledge.
    private float SleepLength => _sleeping.Height * 0.5f;

    //
    // THE SPRING'S SETTINGS PER STATE
    //

    // Soft and barely damped, so the sway carries on instead of dying out
    private const float IDLE_STIFFNESS = 55f;
    private const float IDLE_DAMPING = 2.2f;

    // Firmer while it coils - it has to hold itself compressed, not sag
    private const float COIL_STIFFNESS = 90f;
    private const float COIL_DAMPING = 7f;

    // The snap. Stiff and almost undamped is what makes it whip out.
    private const float STRIKE_STIFFNESS = 700f;
    private const float STRIKE_DAMPING = 3.5f;

    // Coming back it is heavily damped, so it settles instead of pinging
    // about the platform for a second and a half
    private const float RECOVER_STIFFNESS = 120f;
    private const float RECOVER_DAMPING = 14f;

    //
    // THE IDLE SWAY
    //
    // A push left, then right, then left, at the rate the spring likes to
    // swing at anyway. The bounce is the spring's, not an animation's.
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
    // It has to reach the MIDDLE of the ledges around it - stand on the one
    // next door and the flower can still get you, which is the whole threat
    // of it. Platforms are placed by how far the player can jump, and that
    // gap is different every time, so the distance is MEASURED off the actual
    // neighbours rather than typed in as a number that would be too short on
    // one layer and silly on the next.
    //
    // Its own ledge, plus this much past either end, is the floor - a flower
    // on a lonely platform still covers the ground it is standing on.
    private const float WAKE_MARGIN = 50f;

    // How many ledges around it count as "surrounding". Two is the one before
    // it and the one after it on the climb.
    private const int NEIGHBOUR_LEDGES = 2;

    // Whatever the neighbours say, it never gets longer than this. Stops one
    // oddly placed platform turning a flower into a screen-wide hazard.
    private const float REACH_CAP = 520f;

    // The ground slab is a platform too, and its middle is half a screen away.
    // Anything this wide is scenery to walk on, not a ledge to be bitten on.
    private const float MAX_LEDGE_WIDTH = 500f;

    // Where the leaves sit: the fixed end of the constraint, on the platform
    private Vector2 root;

    // The strip of air just above its platform - standing here wakes it
    private Rectangle trigger;

    //
    // PLUCKING
    //
    // The ledge it is rooted to. Standing on THAT ledge - not near it, not on
    // the one next door - is what puts the flower in reach of a bare hand.
    private Platform ledge;

    // How far past either end of the ledge still counts as standing on it, so
    // the prompt does not blink out when the player is half off the edge
    private const float LEDGE_MARGIN = 24f;

    // And how far his feet may be off the top of it. A few pixels of slack,
    // because he settles onto a platform rather than landing exactly on it.
    private const float FOOT_SLACK = 12f;

    protected override AudioId? HitSound => AudioId.BatSqueak;

    // Only counts once it has actually woken up. A flower on a ledge the player
    // never touched must not stop him from finishing the layer.
    public override bool CountsAsEnemy => state != FlowerState.Sleeping;

    // The head is mostly petals, and the bite is the middle of it
    protected override Vector2 HitboxScale => new(0.70f, 0.70f);

    public Flower()
    {
        // Tough on purpose. Shooting one down is meant to be a real spend of
        // ammunition and standing still, so that plucking it - which costs a
        // trip onto its ledge instead - stays worth the risk.
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

    // Far enough to bite someone standing in the middle of the ledges next to
    // it, and no further. Walks out from its own platform to the nearest few
    // neighbours and takes the furthest of them, so a flower with a ledge
    // right beside it stays a short one and a flower across a long jump grows
    // the stalk it needs to cover that jump.
    //
    // Distances are measured to the TOP MIDDLE of each neighbour - the spot
    // the player actually stands on, not the middle of the platform sprite.
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

        // BEFORE ANYTHING ELSE, because a plucked flower has no behaviour left
        // to run this frame
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

        // THE CONSTRAINT RUNS LAST. Everything above only ever changed what
        // the spring is pulling towards - this is the one line that decides
        // where the head actually ends up this frame.
        _spring.Update(Globals.DT);

        PlaceHead();

        Sprite.Update();
        _leaves.Update();
        _stem.Update();
    }

    //
    // PLUCKING
    //

    // Shows the prompt while the player is standing on this flower's ledge, and
    // takes the flower off the platform if he presses the key while it is up.
    // Returns true only on the frame it was actually plucked.
    //
    // A player already carrying a bomb is never offered a second one, and the
    // world takes his key press off him first to put the one he has down - so
    // the prompt correctly disappears the moment his hands are full.
    private bool OfferPluck(IPlayer player)
    {
        if (player.HasBomb || !StandingOnMyLedge(player))
            return false;

        // The HUD writes it along the bottom of the screen - the flower only
        // says that the offer is on, not where the words go
        GamePlayUI.OfferPluck();

        if (!player.TryTakeInteract())
            return false;

        // Pulled up by the roots. It dies the same way it would to a bullet,
        // so the layer, the score and the mob list all handle it as a kill.
        HP = 0;

        player.GiveBomb();

        return true;
    }

    // On THIS ledge: feet on its top edge, and somewhere between its ends.
    // Being in the air over it does not count - the flower has to be reachable
    // by hand, and you cannot pluck something on your way past it.
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
        if (!trigger.Intersects(player.Bounds) && !WithinOf(player, reach))
            return;

        EnterState(FlowerState.Waking);

        // Cannot be shot while it is coming up, so it always gets one bite off
        Invulnerable = true;
        SwapHead(_idle);
    }

    private void Waking()
    {
        // The stalk grows: the rest length is walked out from nothing to its
        // idle height and the spring drags the head up after it, overshooting
        // a little at the top because it is barely damped. That overshoot is
        // the flower springing open, and nothing animates it.
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

    //
    // DRAWING
    //
    // Bottom up: leaves, then the stalk, then the head on top of the join.
    // The spring itself is never drawn - the repeated stem IS the spring, as
    // far as the player is concerned.
    //

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
