using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

public interface IMob : IDrawableLayer
{
    Rectangle Bounds { get; set;}
    int HP { get; set; }
    int MaxHP { get; }
    int DAMAGE { get; set; }
    int PointsOnKill { get; set; }
    bool HittingPlayer { get; set;}
    OnTouch ON_TOUCH { get; set; }

    // Whether this mob has to die before the layer counts as cleared.
    // False for things that are not really threats, so they cannot stop the
    // player finishing a layer (a butterfly, or a flower nobody stepped on).
    bool CountsAsEnemy { get; }

    // Whether a player bullet is spent on this mob. False for the two mobs
    // that are not shot at all - shots fly straight through a jellyfish and a
    // butterfly instead of being swallowed by them.
    bool StopsBullets { get; }

    // Whether killing this feeds the life steal gift. False for the mobs that
    // die of their own accord: a jellyfish burning its fuse out and a butterfly
    // finishing its heal are not kills the player earned.
    bool GivesLifeSteal { get; }

    // The one mob a boss layer is actually about. Killing it clears the layer,
    // whatever its escort is still doing - see Layer.Update.
    bool IsBoss { get; }

    void LoadContent(Vector2 playerPos, int spawnLayer);
    void Update(IPlayer player, GameWorld gameWorld);
    void TakeDamage(int damage, IAudioService audio);

    // Frozen where it stands for this long - the stun gun's whole point
    void Stun(float seconds);

    // Shoved along the given direction and left to walk back - the rifle's
    // whole point. Force is in pixels per second and bleeds off by itself.
    void Knockback(Vector2 direction, float force);
    void BounceFromFloor();
    void Explode();
    void MoveTo(Vector2 position);

    // The same thing, given the middle of the mob rather than its top left
    void MoveCentreTo(Vector2 centre);
    void ScaleHP(float scale);

    // The chosen difficulty, folded into this mob's HP and damage. Called once
    // as it joins a layer, and given the layer it is joining because a boss
    // grows with the climb and an ordinary mob does not.
    void ApplyDifficulty(int layerIndex);

    // Hit tests. A mob answers with its polygon outline if it has one,
    // otherwise with its rectangle. The Polygon one is what the player is
    // asked with, so an outline meets an outline where both have one.
    bool CollidesWith(Rectangle rect);
    bool CollidesWith(Circle circle);
    bool CollidesWith(Polygon polygon);

    // Only for the debug overlay - null for mobs that use a plain box
    Polygon? HitboxPolygon { get; }
}
