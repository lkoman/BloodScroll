using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// THE MOTH'S HIDING PLACE
//
// Hangs near the ceiling and does nothing on its own. Not an enemy and not a
// target - CollidesWith says no to everything, so bullets pass through.
//
// All it does is open and shut. SHUT means the moth is inside, healing and out
// of reach. Once she is dead it hangs open and faded - see DEAD_ALPHA.
//

public class Cocoon : MobBase
{
    private AnimatedSprite _closed, _open;

    // Starts shut, because that is where the moth comes out of
    public bool MothInside { get; set; } = true;

    // WHAT IS LEFT OF IT AFTER THE FIGHT. It hangs dead centre for the rest of
    // the layer, right where the player has to climb, so it goes TRANSLUCENT the
    // moment she dies rather than hiding the platforms behind it.
    private const float DEAD_ALPHA = 0.5f;

    // Looked for once and then KEPT, including after she is killed - the layer
    // drops her from its mob list at 0 HP, so a reference to a moth on 0 HP is
    // how we know she is gone. Re-asking the layer would also say "not there"
    // every time she chases the player up a screen.
    private Moth moth;
    private bool lookedForMoth = false;

    private bool MothDead => moth != null && moth.HP <= 0;

    // Faded once she is dead, and normal every other moment of the fight
    protected override Color DrawColour => Tinted(Color.White) * (MothDead ? DEAD_ALPHA : 1f);

    // Furniture, not a fight - never counts towards clearing the layer or score
    public override bool CountsAsEnemy => false;

    protected override Vector2 HitboxScale => new(0.70f, 0.85f);

    public Cocoon()
    {
        // HP it can never lose - a mob is dropped from the layer at 0 HP and this
        // one has to still be hanging there at the end of the fight
        SetHP(100);

        // In front of the moth (every other mob is on 20), so her flight home
        // reads as tucking in BEHIND it. Still behind the player and his bullets.
        DrawLayer = 21;

        DAMAGE = 0;
        PointsOnKill = 0;
        ON_TOUCH = OnTouch.Nothing;
    }

    public override void LoadContent(Vector2 _, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        _closed = MobArt.MothBoss(MobArt.CocoonClosed);
        _open = MobArt.MothBoss(MobArt.CocoonOpen);

        Sprite = _closed;

        SetSpawn();
        RebuildBounds();
    }

    // Dead centre, flush with the ceiling. NEVER MOVES AGAIN - the moth chases
    // the player up through the layers but her cocoon stays where it was built,
    // so healing costs her the flight back down.
    protected override void SetSpawn()
    {
        Sprite.Position = new Vector2(
            (Core.windowWidth - Sprite.Width) / 2f,
            LayerTopY
        );
    }

    protected override void UpdateBehaviour(IPlayer _, GameWorld gameWorld)
    {
        FindMoth(gameWorld);

        AnimatedSprite want = MothInside ? _closed : _open;

        if (Sprite != want)
        {
            // The two drawings are different widths, so line them up by the
            // MIDDLE or it jumps sideways every time it opens
            Vector2 middle = Middle;

            want.Position = middle - new Vector2(want.Width, want.Height) * 0.5f;
            Sprite = want;

            RebuildBounds();
        }

        Sprite.Update();
    }

    // Looked for on the layer it was hung on, which is where she is on the frame
    // this runs. She may take the fight several screens up afterwards; the
    // reference does not care. A cocoon on a layer with no moth stays solid.
    private void FindMoth(GameWorld gameWorld)
    {
        if (lookedForMoth)
            return;

        moth = gameWorld.FindMobOnLayer<Moth>(SpawnLayer);
        lookedForMoth = true;
    }

    // Where the moth aims for, and what it tucks itself behind
    public Vector2 Middle => Sprite.Position + new Vector2(Sprite.Width, Sprite.Height) * 0.5f;

    // Nothing touches it: not the player, not his bullets, not the floor
    public override bool CollidesWith(Rectangle rect) => false;
    public override bool CollidesWith(Circle circle) => false;
    public override bool CollidesWith(Polygon polygon) => false;

    public override void TakeDamage(int damage, IAudioService audio) {}

    public override void BounceFromFloor() {}
}
