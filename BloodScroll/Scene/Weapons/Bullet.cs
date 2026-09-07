using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// What a mob projectile does when it reaches the player.
// Player bullets always deal damage.
//
public enum BulletEffect
{
    Damage,
    Slow,

    // The green spider's web. Not a stronger slow - it takes the controls away
    // for a second instead of taxing them.
    Root,

    // The green bat's shot. Hurts on arrival, then keeps taking HP past the
    // shield down to Player.POISON_FLOOR.
    Poison
}

//
// BULLET
// Spawns, updates and draws a bullet
//

public class Bullet: IDrawableLayer
{
    public int DrawLayer { get; set; } = 35;

    //
    // THE SPRITE IS A RULER, NOT A PICTURE
    //
    // Nothing draws it - every shot is a code drawn circle. The atlas region is
    // only its SIZE: the hitbox is measured off the frame and the circle is
    // drawn at that same radius, so what you see is what hits.
    //
    public Sprite _bullet;
    public Circle bulletBounds;
    public int DAMAGE;
    public BulletEffect Effect = BulletEffect.Damage;
    private float SPEED;
    public Vector2 direction = Vector2.Zero;
    public int active = 1;
    public bool bounce = false;

    //
    // WHAT THIS PARTICULAR SHOT DOES
    //
    // A plain bullet leaves all of these alone. The two slow guns set them after
    // LoadContent, so one class covers every shot in the game.
    //

    // The gun, its shot and its cooldown bar all wear the same colour. For a mob
    // this is the ONLY thing saying which one shot at you.
    public Color Tint = Color.White;

    // A SHELL does no damage where it lands - it goes off and the blast hurts.
    // See GameWorld.Detonate.
    public bool Explosive = false;
    public float BlastRadius = 0f;

    // Seconds until it goes off by itself, wherever it has got to. 0 means it
    // has no fuse and flies until it hits something or leaves the world.
    public float Fuse = 0f;
    public bool FuseSpent { get; private set; } = false;

    // Ends the fuse early. Downstream this is identical to the fuse running out
    // on its own - whoever watches FuseSpent detonates it.
    public void CutFuse()
    {
        Fuse = 0f;
        FuseSpent = true;
    }

    // How long whatever it hits stays frozen. 0 for everything but the stun gun.
    public float StunSeconds = 0f;

    // How hard whatever it hits is shoved, in pixels per second, along the way
    // this shot was flying. 0 for everything but the rifle - see MobBase.Knockback.
    public float Knockback = 0f;

    // Where the shot actually is - the middle, not the corner of the frame.
    // Blasts are measured from here.
    public Vector2 Centre => _bullet.Position + new Vector2(_bullet.Width, _bullet.Height) * 0.5f;

    // A WEB SHOT. The same orb web the player gets wrapped in, only small - see
    // WebTexture, there is no web art in the atlas.
    //
    // The sprite underneath is still what the hitbox is measured from; it is
    // just stretched to the web drawn in its place.
    private const int WEB_SIZE = 48;
    private bool IsWeb => Effect == BulletEffect.Slow || Effect == BulletEffect.Root;

    // THE BORDER. Every circle is a pale rim with the colour inside it, so a
    // dark shot still has a bright edge against a busy background.
    private const float BORDER_SHARE = 0.16f;
    private const int BORDER_MIN = 2;

    // How far towards white the rim is walked. Short of white itself, or every
    // shot looks the same from a distance.
    private const float BORDER_LIGHTEN = 0.6f;

    // A rooting web is the same strands lit up - the ONE warning that this shot
    // nails the player down rather than slowing him.
    private Color WebColour => Effect == BulletEffect.Root ? Globals.RootWeb : Color.White;

    public Bullet() {}

    public void LoadContent(Vector2 spawn, Vector2 target, string bulletType, int damage, float speed, BulletEffect effect = BulletEffect.Damage, float scale = 1f)
    {
        _bullet = Globals.Weapons.CreateSprite(bulletType);

        DAMAGE = damage;
        SPEED = speed;
        Effect = effect;

        // The hitbox is measured off the sprite, so a shot drawn bigger HITS
        // bigger
        if (scale != 1f)
            _bullet.Scale = new Vector2(scale, scale);

        if (IsWeb)
            _bullet.Scale = new Vector2(
                (float)WEB_SIZE / _bullet.Region.Width,
                (float)WEB_SIZE / _bullet.Region.Height);

        // Poison shots are always green, whoever fired them
        if (Effect == BulletEffect.Poison)
            Tint = Globals.PoisonGreen;

        _bullet.Position = spawn;

        direction = Vector2.Normalize(target - spawn);

        bulletBounds = CollisionManager.SetBoundingCircle(_bullet);
    }

    public void Update()
    {
        _bullet.Position = MovementUtils.MoveForward(_bullet.Position, direction, SPEED);

        BurnFuse();

        // IF BULLET IS THREE SCREENS AWAY IN EVERY DIRECTION, IT DISSAPEARS
        float left   = -Globals.CameraOffset.X - Core.windowWidth  * 3;
        float right  = -Globals.CameraOffset.X + Core.windowWidth  * 3;
        float top    = -Globals.CameraOffset.Y - Core.windowHeight * 3;
        float bottom = -Globals.CameraOffset.Y + Core.windowHeight * 3;

        if (_bullet.Position.X < left ||
            _bullet.Position.X > right ||
            _bullet.Position.Y < top ||
            _bullet.Position.Y > bottom)
        {
            active = 0;
        }

        bulletBounds = CollisionManager.UpdateBoundingCircle(bulletBounds, _bullet);
    }

    //
    // STEPPING BACK OUT OF WHAT IT JUST HIT
    //
    // A bounce only turns the shot around, it does not move it - so without this
    // the next frame finds it still inside the ledge and spends its one bounce.
    //
    // Bounds are refreshed here rather than in the next Update, because the rest
    // of THIS frame's platforms are still to be tested against them.
    //
    public void PushOut(Vector2 away)
    {
        _bullet.Position += away;
        bulletBounds = CollisionManager.UpdateBoundingCircle(bulletBounds, _bullet);
    }

    // A shell keeps flying while its fuse burns, then goes off where it is
    private void BurnFuse()
    {
        if (Fuse <= 0f)
            return;

        Fuse -= Globals.DT;

        if (Fuse <= 0f)
            FuseSpent = true;
    }

    public void Draw()
    {
        if (IsWeb)
        {
            DrawWeb();
            return;
        }

        DrawCircle();
    }

    // A DISC IN A PALER DISC, drawn at the bounding circle's own radius - so the
    // shot on screen IS the hitbox.
    private void DrawCircle()
    {
        int diameter = bulletBounds.Radius * 2;
        int border = Math.Max(BORDER_MIN, (int)MathF.Round(diameter * BORDER_SHARE));

        Color fill = ShotColour();

        DrawDisc(diameter, Color.Lerp(fill, Color.White, BORDER_LIGHTEN));
        DrawDisc(diameter - border * 2, fill);
    }

    // Centred on the bullet, the same way the web is, so the two sizes sit
    // inside one another however big the shot is
    private void DrawDisc(int size, Color colour)
    {
        Texture2D disc = BulletTexture.Get(size);

        Globals.SpriteBatch.Draw(
            disc,
            Centre,
            null,
            colour,
            0f,
            new Vector2(disc.Width / 2f, disc.Height / 2f),
            1f,
            SpriteEffects.None,
            0f
        );
    }

    // A lit shell flashes towards white, faster the nearer it is to going off.
    // Same telegraph as the butterfly's fuse. Everything else is its own colour.
    private Color ShotColour()
    {
        if (!Explosive || FuseSpent)
            return Tint;

        float pulse = 0.5f + 0.5f * MathF.Sin(Fuse * 24f);

        return Color.Lerp(Tint, Color.White, pulse * 0.7f);
    }

    // Centred on the bullet, so the strands sit exactly where the hitbox is
    private void DrawWeb()
    {
        Texture2D web = WebTexture.Get(WEB_SIZE);

        Globals.SpriteBatch.Draw(
            web,
            _bullet.Position + new Vector2(_bullet.Width, _bullet.Height) * 0.5f,
            null,
            WebColour,
            0f,
            new Vector2(web.Width / 2f, web.Height / 2f),
            1f,
            SpriteEffects.None,
            0f
        );
    }
}