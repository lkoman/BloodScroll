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
// hanging open for the rest of the layer, and FADED - see DEAD_ALPHA.
//

public class Cocoon : MobBase
{
    private AnimatedSprite _closed, _open;

    // Starts shut, because that is where the moth comes out of
    public bool MothInside { get; set; } = true;

    //
    // WHAT IS LEFT OF IT AFTER THE FIGHT
    //
    // With the moth dead the cocoon has said everything it has to say, but it
    // is hung dead centre against the ceiling and it stays hung there for the
    // rest of the layer - right where the player now has to climb. Solid, it
    // is furniture that hides the platforms he is aiming for.
    //
    // So it goes translucent the moment she dies. Still there, because the
    // room should keep the evidence of what happened in it; no longer able to
    // swallow a ledge.
    private const float DEAD_ALPHA = 0.5f;

    // The moth this one belongs to, found the same way she finds it. Looked
    // for once and then kept, INCLUDING AFTER SHE IS KILLED - the layer drops
    // her from its mob list the moment her HP hits zero, so a reference to a
    // moth on nought HP is exactly how we know she is gone. Asking the layer
    // again would only tell us she is not there, which is also true every time
    // she chases the player up a screen.
    private Moth moth;
    private bool lookedForMoth = false;

    private bool MothDead => moth != null && moth.HP <= 0;

    // Faded once she is dead, and normal every other moment of the fight
    protected override Color DrawColour => Tinted(Color.White) * (MothDead ? DEAD_ALPHA : 1f);

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

    protected override void UpdateBehaviour(IPlayer _, GameWorld gameWorld)
    {
        FindMoth(gameWorld);

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

    // Looked for on the layer it was hung on, which is where she starts and
    // where she is on the frame this runs. She may take the fight several
    // screens up from here afterwards; the reference does not care.
    //
    // A cocoon on a layer with no moth on it simply never finds one, and stays
    // solid for good - which is the right answer to a question nobody asked.
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

    public override void TakeDamage(int damage, IAudioService audio) {}

    public override void BounceFromFloor() {}
}
