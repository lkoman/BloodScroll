using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// THE MOTH'S HIDING PLACE
//
// Hangs near the ceiling and does nothing on its own. It is not an enemy and it
// is not a target: it cannot be hurt, it cannot be killed, and bullets go
// straight through it. Shooting at it is simply wasted ammunition, which is why
// CollidesWith says no to everything rather than eating the shot.
//
// All it does is open and shut. Shut means the moth is inside, healing, and
// out of reach - that is the whole tell. Once the moth is dead it is left
// hanging open for the rest of the layer.
//

public class Cocoon : MobBase
{
    private AnimatedSprite _closed, _open;

    // Starts shut, because that is where the moth comes out of
    public bool MothInside { get; set; } = true;

    // It is furniture, not a fight. It never counts towards clearing the layer
    // and it never counts towards the score.
    public override bool CountsAsEnemy => false;

    protected override Vector2 HitboxScale => new(0.70f, 0.85f);

    public Cocoon()
    {
        // HP it can never lose. A mob is taken off the layer the moment its HP
        // hits zero, and this one is meant to still be hanging there at the end
        // of the fight, so it keeps a value it has no way of spending.
        SetHP(100);

        // In front of the moth, which is the same 20 every other mob is on, so
        // her flight home reads as tucking in BEHIND it rather than landing on
        // top of it. Still behind the player and his bullets.
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

    // Dead centre, flush with the ceiling. It is the first thing in the room
    // and the thing the whole fight is about, so it is hung where it cannot be
    // missed rather than tucked off to one side.
    //
    // It never moves again after this. The moth chases the player up through
    // the layers, but her cocoon stays where it was built - which is what makes
    // healing cost her the fight: she has to fly all the way back down to it.
    protected override void SetSpawn()
    {
        Sprite.Position = new Vector2(
            (Core.windowWidth - Sprite.Width) / 2f,
            LayerTopY
        );
    }

    protected override void UpdateBehaviour(IPlayer _, GameWorld __)
    {
        AnimatedSprite want = MothInside ? _closed : _open;

        if (Sprite != want)
        {
            // The two drawings are different widths, so line them up by the
            // middle - otherwise it jumps sideways every time it opens
            Vector2 middle = Middle;

            want.Position = middle - new Vector2(want.Width, want.Height) * 0.5f;
            Sprite = want;

            RebuildBounds();
        }

        Sprite.Update();
    }

    // Where the moth aims for, and what it tucks itself behind
    public Vector2 Middle => Sprite.Position + new Vector2(Sprite.Width, Sprite.Height) * 0.5f;

    // Nothing touches it: not the player, not his bullets, not the floor
    public override bool CollidesWith(Rectangle rect) => false;
    public override bool CollidesWith(Circle circle) => false;

    public override void TakeDamage(int damage, IAudioService audio) {}

    public override void BounceFromFloor() {}
}
