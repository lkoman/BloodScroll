using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// BOSS - MOTH
//
// She is pinned to the screen by the middle of her body, the same way the
// spider queen is: she never flips left or right, she TURNS, and her head
// follows the player wherever he goes. Every attack she has is fired down the
// line she is looking along at the moment she commits, so the turn is the tell
// and stepping out of that line is the whole defence.
//
// She has exactly two attacks:
//
//   THE WING GUST - she stops dead, turns to look at you, beats her wings and
//       throws a wall of air down that line. It hurts, and more importantly it
//       throws you off whatever you were standing on. Four of these in a row.
//   THE RAM - after the fourth gust she lines up properly and charges, body
//       first, exactly like the queen's lunge.
//
// Then there is the cocoon. She flies home, shuts herself in and heals hard -
// a long hide is worth close to half her bar. While she is in there she cannot
// be touched at all, so it is dead air the player can do nothing about except
// have hit harder before it started.
//
// She does NOT run home after every ram. Below HALF her health the ram is what
// sends her back, and on top of that she can break off between any two attacks
// on a coin toss - so the player can never time the healing.
//
// The cocoon never moves. She chases the player up through the layers, but the
// cocoon stays hanging in the arena she started in, so the further he has
// dragged her the longer the round trip home costs her.
//

public class Moth : MobBase
{
    private enum Phase { Fighting, Returning, Hidden }

    // What she is doing while she is out
    private enum Move { Hovering, Aiming, Flapping, Recovering, Ramming }

    private AnimatedSprite _idle, _flap;

    private Phase phase = Phase.Fighting;
    private Move move = Move.Hovering;
    private float moveTimer = 0f;
    private float phaseTimer = 0f;

    //
    // WHICH WAY THE ART FACES
    //
    // Everything below turns her so her head points at the player, which means
    // the code has to know where the head is in the drawing BEFORE anything is
    // turned. Change this one line to match what you draw:
    //
    //   head drawn pointing UP    -> -MathHelper.PiOver2   (top down moth)
    //   head drawn pointing RIGHT ->  0f                   (side on moth)
    //
    // Draw her with the body centred in the frame - the middle of the frame is
    // the pin she spins on, so a body drawn off to one side will swing around
    // instead of turning on the spot.
    //
    private const float ART_FACING = -MathHelper.PiOver2;

    // Where her head points in world space, radians
    private float facing = 0f;

    private const float TURN_HOVERING = 1.4f;   // radians a second while she drifts
    private const float TURN_AIMING = 3.4f;     // and while she lines an attack up
    private const float AIM_TOLERANCE = 0.10f;  // how straight is straight enough

    //
    // THE CYCLE
    //
    // Four gusts, then a ram, then round again. Long enough between them that
    // each one reads as an event rather than as weather.
    //
    private const int GUSTS_PER_VOLLEY = 4;
    private int gustsFired = 0;

    // True from the end of a ram until the next gust goes out, so the low
    // health retreat hangs off the ram and not off every single attack
    private bool rammedLast = false;

    private const float HOVER_SECONDS = 1.6f;    // drifting, between attacks
    private const float FLAP_SECONDS = 0.5f;     // wings beating, gust about to go
    private const float RECOVER_SECONDS = 0.35f; // caught out in the open afterwards

    // She holds the aim for at least this long even when she was already
    // looking straight at him. The turn is the only warning an attack gives,
    // and a warning nobody had time to see is not a warning. The ram gets a
    // longer one because it is the one that can take half your health.
    private const float AIM_MIN_SECONDS = 0.35f;
    private const float RAM_AIM_SECONDS = 0.6f;

    // She fires anyway once this runs out, so circling her forever is not a way
    // to keep her harmless
    private const float AIM_MAX_SECONDS = 1.5f;

    private const float RAM_SECONDS = 0.9f;
    private const float RAM_SPEED = 1150f;

    private const float HOVER_SPEED = 260f;
    private const float RETURN_SPEED = 520f;

    // How fast she stops drifting when she plants herself to aim
    private const float SETTLE = 0.82f;

    // The line she committed to. Everything she throws goes here, not at
    // wherever the player has got to since - so it can be dodged on the windup.
    private Vector2 committed = Vector2.UnitX;
    private bool ramNext = false;
    private bool hitTheFloor = false;

    private Vector2 target = Vector2.Zero;

    private const int GUST_DAMAGE = 55;
    private const float GUST_KNOCKBACK = 1300f;

    private readonly MothGust gust = new();

    //
    // HEALING
    //
    private const float HIDE_MIN_SECONDS = 1f;
    private const float HIDE_MAX_SECONDS = 5f;
    private float hideSeconds = 0f;

    // Real healing, not a trickle. The longest hide is worth close to half her
    // bar, so a hide the player lets her finish genuinely undoes a chunk of the
    // fight - the pressure is on getting her down before she can bank it.
    private const int REGEN_PER_SECOND = 110;
    private float regenCarry = 0f;

    // Below this she starts using the ram as her way out. Half her bar, so the
    // back half of the fight is a race against the cocoon.
    private const float HURT_FRACTION = 0.5f;

    // ...and at any other point between two attacks, this often. Rolled once
    // per attack, so roughly every three seconds.
    private const double HIDE_CHANCE = 0.35;

    private Cocoon cocoon;
    private int cocoonLayer = 0;
    private bool lookedForCocoon = false;

    protected override bool ShowBossHP => true;
    protected override AudioId? HitSound => AudioId.BatSqueak;

    protected override Vector2 HitboxScale => new(0.65f, 0.70f);

    // Wings spread wide to either side of a narrow body, which a rectangle
    // cannot express at all - a box round this frame is mostly the empty
    // corners above the wingtips. This traces the silhouette instead, in
    // fractions of the sprite frame, clockwise from the top of the head. It
    // turns with her, so it stays on the drawing at every angle.
    protected override Vector2[] HitboxShape =>
    [
        new(0.50f, 0.18f),
        new(0.75f, 0.26f),
        new(0.99f, 0.42f),
        new(0.90f, 0.70f),
        new(0.62f, 0.95f),
        new(0.38f, 0.95f),
        new(0.10f, 0.70f),
        new(0.01f, 0.42f),
        new(0.25f, 0.26f),
    ];

    public Moth()
    {
        SetHP(1000);
        DAMAGE = 70;
        PointsOnKill = 1500;
    }

    public override void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        _idle = MobArt.MothBoss(MobArt.Moth);
        _flap = MobArt.MothBoss(MobArt.MothAttack);

        Sprite = _idle;

        SetSpawn();
        target = playerPos;

        speed = HOVER_SPEED;
        max_speed = HOVER_SPEED * 1.2f;

        // Looking down into the arena to begin with
        facing = MathHelper.PiOver2;
        SpriteRotation = facing - ART_FACING;

        // The gust is built as wide as the moth that makes it
        gust.LoadContent(Sprite.Width);

        FollowFrameSize();
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        FindCocoon(gameWorld);

        // The cocoon is shut exactly when she is inside it, and it is left
        // however it last was when she dies - which is open, because she can
        // only ever be killed while she is out
        if (cocoon != null)
            cocoon.MothInside = phase == Phase.Hidden;

        // Air she has already blown keeps travelling whatever she does next
        UpdateGust(player, gameWorld);

        phaseTimer += Globals.DT;

        // She does not stay behind in an empty arena - see ClimbingAfterPlayer.
        // Only ever while she is out fighting: a trip home to the cocoon is
        // hers to finish, and the player moving again does not interrupt it.
        if (phase == Phase.Fighting && ClimbingAfterPlayer(player))
        {
            Sprite.Update();
            FollowFrameSize();
            SyncBounds();
            return;
        }

        switch (phase)
        {
            case Phase.Fighting: Fighting(player); break;
            case Phase.Returning: Returning(player); break;
            case Phase.Hidden: Hiding(); break;
        }

        // She belongs to the pane of her own screen - she drifts it, she never
        // leaves it, and a ram that would carry her off it stops at the edge.
        //
        // Only while she is fighting, though. The cocoon can be layers below
        // the arena she is holding, and the flight home has to be allowed to
        // cross that gap.
        if (phase == Phase.Fighting)
            ClampToArena();

        Sprite.Update();

        // Her frames are not all the same size, so this has to come after the
        // animation has picked one
        FollowFrameSize();

        SyncBounds();
    }

    // The cocoon is spawned as its own mob, so we go looking for it once
    private void FindCocoon(GameWorld gameWorld)
    {
        if (lookedForCocoon)
            return;

        cocoon = gameWorld.FindMobOnLayer<Cocoon>(SpawnLayer);

        // Where it hangs, for good. SpawnLayer moves with her as she takes new
        // arenas off the player; this does not, so it is what she flies back to.
        cocoonLayer = SpawnLayer;

        lookedForCocoon = true;

        if (cocoon == null)
            return;

        // She is not dropped into the room, she comes OUT of the cocoon that has
        // been hanging there shut since the player walked in. This is her first
        // frame, so putting her on top of it is the whole birth: the cocoon
        // opens the moment she is out (MothInside above reads her phase), and
        // she drifts off it under her own steam.
        Sprite.Position = RestPosition;

        RebuildBounds();
    }

    // No cocoon on the layer means she never gets to stop attacking
    private bool HasShelter => cocoon != null;

    //
    // OUT IN THE OPEN
    //

    private void Fighting(IPlayer player)
    {
        moveTimer += Globals.DT;

        switch (move)
        {
            case Move.Hovering: Hovering(player); break;
            case Move.Aiming: Aiming(player); break;
            case Move.Flapping: Flapping(); break;
            case Move.Ramming: Ramming(); break;
            case Move.Recovering: Recovering(); break;
        }
    }

    private void Hovering(IPlayer player)
    {
        // Drifts between points of her own choosing rather than trailing the
        // player, so the ram is the only thing that ever comes straight at you
        if (DirectionTimerElapsed())
            PickDriftTarget();

        (Sprite.Position, velocity) = MovementUtils.MoveTowardsTarget(Sprite.Position, target, velocity, speed, max_speed);

        // She keeps half an eye on him while she drifts, so the turn before an
        // attack is a short one and reads as her deciding rather than winding up
        TurnFace(player, TURN_HOVERING);

        if (moveTimer < HOVER_SECONDS)
            return;

        // Four gusts and then the ram, over and over
        ramNext = gustsFired >= GUSTS_PER_VOLLEY;

        moveTimer = 0f;
        move = Move.Aiming;

        if (!ramNext)
            SwapSprite(_flap);
    }

    private void Aiming(IPlayer player)
    {
        // Plants herself first - she must not drift while she lines this up, or
        // the attack would not start from where the player watched her aim
        velocity *= SETTLE;
        Sprite.Position += velocity * Globals.DT;

        bool lockedOn = TurnFace(player, TURN_AIMING);

        if (moveTimer < (ramNext ? RAM_AIM_SECONDS : AIM_MIN_SECONDS))
            return;

        if (!lockedOn && moveTimer < AIM_MAX_SECONDS)
            return;

        // Committed to a line. Move out of it now and what comes next goes past.
        committed = MovementUtils.FromAngle(facing);

        moveTimer = 0f;
        move = ramNext ? Move.Ramming : Move.Flapping;

        SwapSprite(ramNext ? _idle : _flap);
    }

    private void Flapping()
    {
        velocity = Vector2.Zero;

        if (moveTimer < FLAP_SECONDS)
            return;

        gust.Release(Middle, Middle + committed);

        gustsFired ++;
        rammedLast = false;

        EnterRecover();
    }

    private void Ramming()
    {
        // Straight down the committed line. She does not steer and she does not
        // turn - the aim she showed you is the aim she keeps.
        velocity = committed * RAM_SPEED;
        Sprite.Position += velocity * Globals.DT;

        if (moveTimer < RAM_SECONDS && !hitTheFloor && !RammedIntoTheWall())
            return;

        // The volley starts over, and this is the ram the low health retreat
        // hangs off
        gustsFired = 0;
        rammedLast = true;
        hitTheFloor = false;

        EnterRecover();
    }

    private void Recovering()
    {
        // Still hanging there for a moment with her wings spent
        velocity *= SETTLE;
        Sprite.Position += velocity * Globals.DT;

        if (moveTimer < RECOVER_SECONDS)
            return;

        if (ShouldHide())
        {
            phase = Phase.Returning;
            phaseTimer = 0f;

            SwapSprite(_idle);
            return;
        }

        EnterHover();
    }

    // NOT after every ram. Below a third of her health the ram is what sends
    // her home; the rest of the time she can simply break off between any two
    // attacks, so healing is never something the player can time or bait.
    private bool ShouldHide()
    {
        if (!HasShelter)
            return false;

        if (rammedLast && HP < MaxHP * HURT_FRACTION)
            return true;

        return Globals.R.NextDouble() < HIDE_CHANCE;
    }

    private void EnterRecover()
    {
        moveTimer = 0f;
        move = Move.Recovering;

        SwapSprite(_idle);
    }

    private void EnterHover()
    {
        moveTimer = 0f;
        move = Move.Hovering;
        velocity = Vector2.Zero;

        PickDriftTarget();

        SwapSprite(_idle);
    }

    private void PickDriftTarget()
    {
        // Anywhere on her own screen, kept off the very edges so she does not
        // spend the drift grinding along a wall
        target = new Vector2(
            Sprite.Width + Globals.R.NextSingle() * (Core.windowWidth - Sprite.Width * 2f),
            LayerTopY + Sprite.Height + Globals.R.NextSingle() * (Core.windowHeight - Sprite.Height * 2f)
        );

        ResetDirectionTimer(1f, 2f);
    }

    // The gust is hers, so she pays out the damage and the throw
    private void UpdateGust(IPlayer player, GameWorld gameWorld)
    {
        gust.Update();

        if (!gust.CaughtPlayer(player))
            return;

        player.TakeDamage(ScaleDamage(GUST_DAMAGE), gameWorld.Audio);
        player.Knockback(gust.Direction, GUST_KNOCKBACK);
    }

    //
    // HOME AND HIDING
    //

    // The flight home, which can now cross layers - the cocoon stays where it
    // was built and she may be several screens above it by the time she wants
    // it. She is NOT invulnerable yet and she is not attacking either, so the
    // whole trip is free damage for a player who follows her down.
    private void Returning(IPlayer player)
    {
        Sprite.Position = MovementUtils.MOVE(Sprite.Position, RestPosition, RETURN_SPEED);

        // Still watching him on the way home
        TurnFace(player, TURN_HOVERING);

        if (Vector2.Distance(Sprite.Position, RestPosition) > 40f)
            return;

        phase = Phase.Hidden;
        phaseTimer = 0f;

        // 1 to 5 seconds. The player cannot count it out, so a fight that
        // looked won can still have one more heal left in it.
        hideSeconds = HIDE_MIN_SECONDS + Globals.R.NextSingle() * (HIDE_MAX_SECONDS - HIDE_MIN_SECONDS);
        regenCarry = 0f;

        // Nothing reaches her in there - see CollidesWith below, which is what
        // actually stops the bullets
        Invulnerable = true;
        velocity = Vector2.Zero;
    }

    private void Hiding()
    {
        // Tucked inside, untouchable, stitching herself back together
        Sprite.Position = RestPosition;
        velocity = Vector2.Zero;

        // Per second rather than per hide, so a short hide is worth less than a
        // long one and no hide is ever worth a full bar
        regenCarry += REGEN_PER_SECOND * Globals.DT;

        int healed = (int)regenCarry;

        if (healed > 0)
        {
            regenCarry -= healed;
            HP = Math.Min(HP + healed, MaxHP);
        }

        if (phaseTimer < hideSeconds)
            return;

        LeaveCocoon();
        EnterHover();
    }

    // Out of the cocoon and back in the fight, with the cycle starting clean
    // from the first gust.
    private void LeaveCocoon()
    {
        phase = Phase.Fighting;
        phaseTimer = 0f;

        Invulnerable = false;
        gustsFired = 0;
        rammedLast = false;

        // The cocoon never moved, so coming out of it puts her back in the
        // arena she started in. If the player has gone on ahead in the meantime
        // ClimbingAfterPlayer takes over from here and she flies up after him.
        SpawnLayer = cocoonLayer;
    }

    // Where she has to put her top left corner for her middle to sit on the
    // middle of the cocoon
    private Vector2 RestPosition =>
        cocoon.Middle - new Vector2(Sprite.Width, Sprite.Height) * 0.5f;

    //
    // CHASING HIM OUT OF THE ARENA
    //
    // A layer keeps running after the player has left it, so without this she
    // would spend the rest of the fight gusting at an empty room a screen below
    // him while he walked away from a boss he never beat.
    //
    // So she flies after him. She stops attacking, goes straight at him through
    // the ceiling, and the moment she is inside his layer she takes it as her
    // arena and picks the fight up there.
    //
    // The cocoon does NOT come with her - it stays hanging wherever it was
    // built. That is what gives healing a price: the further she has chased him
    // up the tower, the longer the flight home and back, and every second of it
    // is a second she is not attacking. See Returning.
    //
    // SpawnLayer is where her arena is, not only where she started, and every
    // arena edge she is held to is measured from it.
    //
    private const float CLIMB_SPEED = 900f;

    private bool ClimbingAfterPlayer(IPlayer player)
    {
        int playerLayer = Globals.CurrentLayerIndex;

        if (SpawnLayer == playerLayer)
            return false;

        SwapSprite(_idle);

        // Straight at him. The arena walls belong to a layer, and right now she
        // is between two of them - only the sides of the screen still hold her.
        Vector2 toPlayer = MiddleOf(player) - Middle;

        if (toPlayer != Vector2.Zero)
            toPlayer.Normalize();

        velocity = toPlayer * CLIMB_SPEED;

        Sprite.Position += velocity * Globals.DT;
        Sprite.Position = new Vector2(
            MathHelper.Clamp(Sprite.Position.X, 0f, Core.windowWidth - Sprite.Width),
            Sprite.Position.Y
        );

        TurnFace(player, TURN_AIMING);

        if (!InsideLayer(playerLayer))
            return true;

        // Arrived. This is her arena now, and she starts it clean - the gust she
        // was halfway through when he ran does not go off the moment she lands.
        SpawnLayer = playerLayer;

        EnterHover();

        return true;
    }

    private bool InsideLayer(int layerIndex)
    {
        float top = -Core.windowHeight * layerIndex;
        return Middle.Y >= top && Middle.Y <= top + Core.windowHeight;
    }

    //
    // AIMING
    //

    // Swings her head towards the player and says whether she is lined up yet.
    // The sprite and the outline hitbox both follow SpriteRotation, so turning
    // her here turns everything.
    private bool TurnFace(IPlayer player, float radiansPerSecond)
    {
        float want = MovementUtils.AngleTo(Middle, MiddleOf(player));

        facing = MovementUtils.TurnTowards(facing, want, radiansPerSecond);
        SpriteRotation = facing - ART_FACING;

        return MathF.Abs(MovementUtils.AngleDifference(facing, want)) <= AIM_TOLERANCE;
    }

    // The pin she turns on, what the aim is measured from, and where the gust
    // leaves her
    private Vector2 Middle => Sprite.Position + new Vector2(Sprite.Width, Sprite.Height) * 0.5f;

    private static Vector2 MiddleOf(IPlayer player) =>
        player.Position + new Vector2(player.Width, player.Height) * 0.5f;

    // A ram that runs out of arena is over, wherever it ran out. Without this
    // she would spend the rest of the ram grinding along the edge of the screen.
    private bool RammedIntoTheWall()
    {
        return Sprite.Position.X <= 0f ||
               Sprite.Position.X >= Core.windowWidth - Sprite.Width ||
               Sprite.Position.Y <= LayerTopY ||
               Sprite.Position.Y >= LayerTopY + Core.windowHeight - Sprite.Height;
    }

    private void ClampToArena()
    {
        Sprite.Position = new Vector2(
            MathHelper.Clamp(Sprite.Position.X, 0f, Core.windowWidth - Sprite.Width),
            MathHelper.Clamp(Sprite.Position.Y, LayerTopY, LayerTopY + Core.windowHeight - Sprite.Height)
        );
    }

    // A ram that reaches the ground ends there. This is called from the
    // collision pass rather than from our own update, so it leaves a note
    // instead of changing state halfway through a frame.
    public override void BounceFromFloor()
    {
        if (phase == Phase.Fighting && move == Move.Ramming)
        {
            hitTheFloor = true;
            return;
        }

        base.BounceFromFloor();
    }

    // Inside the cocoon she is not in the arena at all: the player cannot walk
    // into her and his bullets go straight through, rather than being eaten by
    // a boss he has no way of hurting.
    public override bool CollidesWith(Rectangle rect) =>
        phase != Phase.Hidden && base.CollidesWith(rect);

    public override bool CollidesWith(Circle circle) =>
        phase != Phase.Hidden && base.CollidesWith(circle);

    //
    // DRAWING
    //

    // Her frames are not all the same size, and the wing beat changes size
    // between its own two frames. Position is a TOP LEFT corner, so a taller
    // frame would shove her sideways and leave the hitbox describing the frame
    // before it. Pin the middle instead, and rebuild the outline to the new size.
    private Vector2 frameSize = Vector2.Zero;

    private void FollowFrameSize()
    {
        Vector2 size = new(Sprite.Width, Sprite.Height);

        if (size == frameSize)
            return;

        if (frameSize != Vector2.Zero)
            Sprite.Position += (frameSize - size) * 0.5f;

        frameSize = size;

        RebuildBounds();
    }

    private void SwapSprite(AnimatedSprite next)
    {
        if (Sprite == next)
            return;

        // Swapped about the middle, for the same reason as above
        next.Position = Middle - new Vector2(next.Width, next.Height) * 0.5f;
        Sprite = next;

        frameSize = Vector2.Zero;
        FollowFrameSize();
    }

    public override void Draw()
    {
        // Air already in flight outlives her ducking into the cocoon
        gust.Draw();

        // Nothing to see while she is inside
        if (phase == Phase.Hidden)
            return;

        base.Draw();
    }
}
