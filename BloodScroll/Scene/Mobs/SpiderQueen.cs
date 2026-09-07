using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// BOSS - SPIDER QUEEN
//
// Pinned by the middle of her body: she never flips, she TURNS about that
// point, and her face follows the player.
//
// ONE ATTACK, ON A LOOP:
//
//   AIM      plants her feet and turns to look straight at him. The turn is the
//            tell - once she commits the ram goes where she was pointing.
//   RAM      straight down that line until she hits the floor, a wall, or runs
//            out of ram.
//   RETREAT  backs away, drops a web, lines the next one up from a distance.
//
// Under half HP: faster ram, shorter wind up, shorter retreat.
//
// The webs never damage - they take away your footwork.
//
// She follows the player up through the floor - see ClimbingAfterPlayer.
//

public class SpiderQueen : MobBase
{
    // The whole fight, in order. It loops.
    private enum Stalk { Aiming, Ramming, Retreating }

    private AnimatedSprite _idle, _aim, _lunge;
    private Stalk stalk = Stalk.Retreating;

    private readonly List<Web> webs = [];

    // Enough to make the floor a problem, not so many the fight is unplayable
    private const int MAX_WEBS = 12;
    private const float WEB_LIFE_SECONDS = 14f;

    //
    // WHICH WAY THE ART FACES
    //
    // The code turns her face towards the player, so it needs to know where the
    // face points in the drawing BEFORE any rotation:
    //   UP -> -PiOver2,  RIGHT -> 0f,  DOWN -> PiOver2
    //
    // Draw the body CENTRED in the frame - the middle is the pin she spins on.
    //
    private const float ART_FACING = -MathHelper.PiOver2;

    // Where her face points in world space, radians
    private float facing = 0f;

    private const float TURN_WALKING = 1.3f;  // radians a second while she backs off
    private const float TURN_AIMING = 3.4f;   // and while she lines a ram up
    private const float AIM_TOLERANCE = 0.10f; // how straight is straight enough

    // She holds the aim for at least this long even when she was already
    // looking straight at him. The turn is the only warning the ram gives, and
    // a warning nobody had time to see is not a warning.
    private const float AIM_MIN_SECONDS = 0.45f;

    // She rams anyway once this runs out, so orbiting her forever is not a way
    // to keep her harmless
    private const float AIM_MAX_SECONDS = 1.6f;

    private const float RAM_SECONDS = 0.9f;

    // A ram that STARTS against a wall or the floor would end on its first
    // frame and leave her twitching in the corner. Every ram gets at least this
    // long to get clear of whatever it began on.
    private const float RAM_MIN_SECONDS = 0.12f;

    private float stalkTimer = 0f;

    private const float RAM_SPEED = 800f;

    // How fast she plants her feet when she stops to aim
    private const float SETTLE = 0.82f;

    private Vector2 ramDirection = Vector2.Zero;
    private bool hitTheFloor = false;

    // BACKING OFF. Any one of the three ends the retreat: far enough, long
    // enough, or a wall at her back.
    private const float RETREAT_SPEED = 420f;
    private const float RETREAT_SECONDS = 1.1f;
    private const float RETREAT_DISTANCE = 620f;

    private Vector2 retreatDirection = Vector2.Zero;

    // UNDER HALF HP. One number, because it is the same attack - it just
    // arrives sooner and hits harder to get out of the way of.
    private bool Enraged => HP * 2 < MaxHP;
    private float Rush => Enraged ? 1.35f : 1f;

    protected override bool ShowBossHP => true;
    protected override AudioId? HitSound => AudioId.BatSqueak;

    protected override Vector2 HitboxScale => new(0.65f, 0.70f);

    // A spider is a fat body with legs sticking out, so a box either swallows
    // the empty space between the legs or clips the body. This traces the
    // silhouette instead - fractions of the sprite frame, clockwise from the
    // face. It turns with her, so it stays on the drawing at every angle.
    protected override Vector2[] HitboxShape =>
    [
        new(0.50f, 0.12f),
        new(0.85f, 0.35f),
        new(0.95f, 0.70f),
        new(0.50f, 0.92f),
        new(0.05f, 0.70f),
        new(0.15f, 0.35f),
    ];

    public SpiderQueen()
    {
        // THE FIRST BOSS IN THE GAME, and the player meets her holding the
        // starter pistol and nothing else. She is here to teach the tell, not
        // to outlast him - the fireboss on layer 5 is the one with the HP bar
        // that hurts. BossTally grows her on every later meeting anyway.
        SetHP(750);

        // THE RAM HITS OFTEN, SO IT MUST NOT HIT HARD - she is the fastest thing
        // in the game and attacks without pause
        DAMAGE = 55;
        PointsOnKill = 1500;
    }

    public override void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        _idle = MobArt.Spiders(MobArt.SpiderQueen);
        _aim = MobArt.Spiders(MobArt.SpiderQueenAim);
        _lunge = MobArt.Spiders(MobArt.SpiderQueenLunge);

        Sprite = _idle;

        SetSpawn();

        // Looking down into the arena to begin with
        facing = MathHelper.PiOver2;
        SpriteRotation = facing - ART_FACING;

        // She drops in and backs off before her first ram, so the fight opens
        // with the wind up rather than with a hit
        retreatDirection = MovementUtils.FromAngle(facing);
        stalk = Stalk.Retreating;

        RebuildBounds();
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        // She does not stay behind in an empty arena - see ClimbingAfterPlayer
        if (ClimbingAfterPlayer(player))
        {
            UpdateWebs(player);
            SyncBounds();

            Sprite.Update();
            return;
        }

        stalkTimer += Globals.DT;

        switch (stalk)
        {
            case Stalk.Aiming: Aiming(player); break;
            case Stalk.Ramming: Ramming(player); break;
            case Stalk.Retreating: Retreating(player); break;
        }

        UpdateWebs(player);

        // She is stuck to the pane of the screen - she walks it, she never
        // leaves it, and a ram that would carry her off it stops at the edge
        ClampToArena();

        SyncBounds();

        Sprite.Update();
    }

    //
    // AIM
    //

    private void Aiming(IPlayer player)
    {
        // Plants her feet first - she must not drift while she lines this up,
        // or the ram would not start from where the player watched her aim
        velocity *= SETTLE;
        Sprite.Position += velocity * Globals.DT;

        bool lockedOn = TurnFace(player, TURN_AIMING);

        // Reared up and staring, whether or not she still has turning to do
        if (stalkTimer < AIM_MIN_SECONDS / Rush)
            return;

        if (!lockedOn && stalkTimer < AIM_MAX_SECONDS / Rush)
            return;

        // Committed. The ram runs down the line she is LOOKING along, not at
        // wherever the player has got to since, so it can be side stepped.
        ramDirection = MovementUtils.FromAngle(facing);

        stalkTimer = 0f;
        stalk = Stalk.Ramming;

        SwapSprite(_lunge);
    }

    //
    // RAM
    //

    private void Ramming(IPlayer player)
    {
        // Straight down the committed line. She does not steer, and she does
        // not turn - the aim she showed you is the aim she keeps.
        velocity = ramDirection * RAM_SPEED * Rush;
        Sprite.Position += velocity * Globals.DT;

        bool stopped = stalkTimer >= RAM_MIN_SECONDS && (hitTheFloor || RammedIntoTheWall());

        if (stalkTimer < RAM_SECONDS && !stopped)
            return;

        // Drops a web wherever the ram ended, so every attack costs the player
        // a piece of the floor
        DropWeb(new Vector2(Middle.X, Sprite.Position.Y + Sprite.Height));

        BeginRetreat(player);
    }

    //
    // RETREAT
    //

    private void Retreating(IPlayer player)
    {
        // Straight back down a line picked when the retreat started, so she
        // cannot end up creeping along beside him
        velocity = retreatDirection * RETREAT_SPEED * Rush;
        Sprite.Position += velocity * Globals.DT;

        // Still watching him on the way out, so the aim that follows is a short
        // turn and reads as her deciding rather than winding up
        TurnFace(player, TURN_WALKING);

        bool farEnough = Vector2.Distance(Middle, MiddleOf(player)) >= RETREAT_DISTANCE;

        if (!farEnough && stalkTimer < RETREAT_SECONDS / Rush && !RammedIntoTheWall())
            return;

        BeginAim();
    }

    // Away from wherever he is standing right now
    private void BeginRetreat(IPlayer player)
    {
        Vector2 away = Middle - MiddleOf(player);

        // Standing exactly on top of her - back off along the ram instead of
        // dividing by nothing
        if (away.LengthSquared() < 1f)
            away = -ramDirection;

        if (away == Vector2.Zero)
            away = MovementUtils.FromAngle(facing + MathHelper.Pi);

        away.Normalize();

        retreatDirection = away;

        stalkTimer = 0f;
        stalk = Stalk.Retreating;
        hitTheFloor = false;

        SwapSprite(_idle);
    }

    private void BeginAim()
    {
        stalkTimer = 0f;
        stalk = Stalk.Aiming;
        velocity = Vector2.Zero;

        SwapSprite(_aim);
    }

    //
    // WHEN THE PLAYER RUNS
    //
    // A layer keeps running after the player leaves it, so without this she
    // would ram an empty arena while he walked away from the fight.
    //
    // She stops attacking, climbs straight at him through the ceiling, and takes
    // his layer as her new arena.
    //
    // SpawnLayer is where her ARENA is, not only where she started. Every arena
    // edge is measured from it.
    //

    private const float CLIMB_SPEED = 600f;

    private bool ClimbingAfterPlayer(IPlayer player)
    {
        int playerLayer = Globals.CurrentLayerIndex;

        if (SpawnLayer == playerLayer)
            return false;

        // Reared up on the way, so a queen coming through the floor is read as
        // the boss arriving rather than as a stray mob
        SwapSprite(_lunge);

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

        // Arrived. This is her arena now, and she starts it clean - she backs
        // off the player she just landed on and aims from there, so arriving
        // is never a free hit.
        SpawnLayer = playerLayer;

        BeginRetreat(player);

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

    // Swings her face towards the player and says whether she is lined up yet.
    // The sprite and the outline hitbox both follow SpriteRotation, so turning
    // her here turns everything.
    private bool TurnFace(IPlayer player, float radiansPerSecond)
    {
        float want = MovementUtils.AngleTo(Middle, MiddleOf(player));

        facing = MovementUtils.TurnTowards(facing, want, radiansPerSecond);
        SpriteRotation = facing - ART_FACING;

        return MathF.Abs(MovementUtils.AngleDifference(facing, want)) <= AIM_TOLERANCE;
    }

    // The pin she turns on, and the point she measures her aim from
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

    // A ram that reaches the ground ends there. Called from the collision pass,
    // not our own update, so it leaves a note instead of changing state mid frame.
    //
    // NO BOUNCE: the base flings a mob off the floor every frame it touches, and
    // a boss this size would vibrate on it for a whole retreat. The arena clamp
    // already stops her going through the floor.
    public override void BounceFromFloor()
    {
        if (stalk == Stalk.Ramming)
            hitTheFloor = true;
    }

    //
    // WEBS
    //

    private void DropWeb(Vector2 position)
    {
        webs.Add(new Web(position, WEB_LIFE_SECONDS));

        // Oldest one goes when we hit the cap, so the arena never fully clogs
        if (webs.Count > MAX_WEBS)
            webs.RemoveAt(0);
    }

    private void UpdateWebs(IPlayer player)
    {
        for (int i = webs.Count - 1; i >= 0; i--)
        {
            webs[i].Update();

            if (webs[i].Expired)
            {
                webs.RemoveAt(i);
                continue;
            }

            if (player.HurtBox.Intersects(webs[i].Bounds))
                player.ApplySlow(Player.WEB_SLOW_FACTOR, 0.4f);
        }
    }

    private void SwapSprite(AnimatedSprite next)
    {
        if (Sprite == next)
            return;

        next.Position = Sprite.Position;
        Sprite = next;

        RebuildBounds();
    }

    public override void Draw()
    {
        // Webs first so the queen stands on top of them
        foreach (var web in webs)
            web.Draw();

        base.Draw();
    }
}
