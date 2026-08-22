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

    // The black spider's web. Not a stronger slow - a different thing entirely:
    // it takes the controls away for a second instead of taxing them.
    Root,

    // The green bat's shot. Hurts on arrival and then keeps taking HP for ten
    // seconds afterwards, past the shield, down to a floor it will not cross.
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
    // Nothing draws it any more - every shot is a code drawn circle. What the
    // atlas region is still good for is its SIZE: the hitbox has always been
    // measured off the frame, and the circle is now drawn at exactly that
    // radius, so what you see on screen is the thing that actually hits.
    //
    // That is why the shooters still name a region. It is how big the shot is,
    // and no longer what it looks like.
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
    // A plain bullet leaves all of these alone and behaves exactly as it always
    // has. The two slow guns set them after LoadContent, so one bullet class
    // still covers every shot in the game.
    //

    // What the circle is filled with. The gun, the shot and its cooldown bar
    // all wear the same colour, so a purple streak is obviously the purple gun -
    // and for a mob it is the ONLY thing that says which one shot at you.
    public Color Tint = Color.White;

    // A SHELL rather than a bullet: it does no damage where it lands, it goes
    // off, and the blast is what hurts. Detonation is asked of the world by
    // whoever notices it - see GameWorld.Detonate.
    public bool Explosive = false;
    public float BlastRadius = 0f;

    // Seconds until it goes off by itself, wherever it has got to. 0 means it
    // has no fuse and flies until it hits something or leaves the world.
    public float Fuse = 0f;
    public bool FuseSpent { get; private set; } = false;

    // THE PLAYER SAYING "NOW". Ends the fuse early, which is the same thing to
    // everybody downstream as the fuse running out on its own - whoever is
    // watching FuseSpent detonates it and the shell is spent. Nothing here
    // knows why it was cut, and nothing needs to.
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

    // A WEB SHOT
    //
    // Drawn as the same orb web the player gets wrapped in, only small - not
    // from the atlas, which has no web art in it. Slowing is the only thing a
    // web does and nothing else in the game slows you from a distance, so the
    // effect is what tells a web shot apart from an ordinary one.
    //
    // The sprite underneath is still there, because that is what the hitbox is
    // measured from - it is just stretched to the web that is drawn in its place.
    private const int WEB_SIZE = 48;
    private bool IsWeb => Effect == BulletEffect.Slow || Effect == BulletEffect.Root;

    //
    // THE BORDER
    //
    // A flat disc of one colour disappears into a busy background, and the two
    // shots that most need to be seen coming are the two darkest ones in the
    // game (the shadow twin's violet, the crab's red). So every circle is drawn
    // as a pale rim with the colour sat inside it: whatever the fill is, the
    // outline is bright, and a shot always has an edge to read against the
    // world behind it.
    //
    // Thick enough to see at the smallest bullet in the game and never thick
    // enough to swallow the colour it is supposed to be framing.
    private const float BORDER_SHARE = 0.16f;
    private const int BORDER_MIN = 2;

    // How far towards white the rim is walked. Far enough to be obviously
    // lighter than the fill, short of white itself - a rim gone all the way to
    // white makes every shot in the game look the same from a distance.
    private const float BORDER_LIGHTEN = 0.6f;

    // A rooting web is the same strands lit up. It is the ONE warning that this
    // shot will nail the player down rather than merely slow him, so it is the
    // loudest colour in the game - a web he has to be out of the way of is a
    // web he has to be able to see coming.
    private Color WebColour => Effect == BulletEffect.Root ? Globals.RootWeb : Color.White;

    public Bullet() {}

    public void LoadContent(Vector2 spawn, Vector2 target, string bulletType, int damage, float speed, BulletEffect effect = BulletEffect.Damage, float scale = 1f)
    {
        _bullet = new Sprite();
        _bullet = Globals.Weapons.CreateSprite(bulletType);

        DAMAGE = damage;
        SPEED = speed;
        Effect = effect;

        // The hitbox is measured off the sprite below, so a shot drawn bigger
        // is a shot that HITS bigger - which is the whole difference between
        // the small fast gun and the big slow one
        if (scale != 1f)
            _bullet.Scale = new Vector2(scale, scale);

        if (IsWeb)
            _bullet.Scale = new Vector2(
                (float)WEB_SIZE / _bullet.Region.Width,
                (float)WEB_SIZE / _bullet.Region.Height);

        // A poison shot is green, so it is obvious in the air which of the two
        // bats it came from without having to find the bat that fired it
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

    // A shell keeps flying while its fuse burns. Nothing stops it going off:
    // hit or miss, when the timer runs out it explodes where it is.
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

    //
    // A DISC IN A PALER DISC
    //
    // Drawn at the bounding circle's own radius, so the shot on screen IS the
    // hitbox - the old sprites carried a couple of pixels of transparent
    // padding and every one of them hit slightly wider than it looked.
    //
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

    // A lit shell flashes towards white, faster the nearer it is to going off -
    // the same telegraph the butterfly's fuse uses, so a fuse always reads the
    // same way whoever lit it. Everything else is just its own colour.
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