using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// A BLAST
//
// Everything that goes off in the game ends up here: a shell from the slow gun
// reaching the end of its fuse or hitting something, and a flower bomb the
// player put down. The thing that went off is finished by the time this exists
// - a blast is not a bomb, it is what a bomb leaves behind.
//
// IT HURTS ONCE, ON THE FRAME IT APPEARS, and then it is only a picture. That
// is deliberate: a blast that kept dealing damage for as long as it was drawn
// would hit for a wildly different amount depending on the frame rate, and the
// player could not read how much a bomb costs him. One circle, one hit, done -
// see GameWorld.LandBlast, which is the only thing that reads Damage.
//
// What is left after that is the animation: the fireball grows a little and
// fades out over a third of a second, which is long enough to see where the
// blast reached and short enough not to hide the fight going on inside it.
//

public class Explosion : IDrawableLayer
{
    // Over the mobs and the player, because it is in front of them
    public int DrawLayer { get; set; } = 45;

    private const float SECONDS = 0.35f;

    // The fireball starts a little smaller than its reach and swells past it,
    // so the blast reads as expanding rather than as a circle switching on
    private const float START_SCALE = 0.55f;
    private const float END_SCALE = 1.15f;

    public Circle Bounds { get; private set; }
    public int Damage { get; }

    // A shell out of the player's own gun cannot hurt him - it is his shot.
    // A bomb he put on the floor absolutely can, and the three second fuse is
    // there so he has time to be somewhere else.
    public bool HurtsPlayer { get; }
    public int PlayerDamage { get; }

    // Cleared by the world the moment it has dealt its damage
    public bool NeedsToLand { get; set; } = true;

    private readonly Color colour;
    private readonly int size;
    private float timer = 0f;

    public bool Finished => timer >= SECONDS;

    public Explosion(Vector2 centre, int damage, float radius, Color colour, bool hurtsPlayer = false, int playerDamage = 0)
    {
        Bounds = new Circle((int)centre.X, (int)centre.Y, (int)radius);

        Damage = damage;
        HurtsPlayer = hurtsPlayer;
        PlayerDamage = playerDamage;

        this.colour = colour;
        size = (int)(radius * 2f);
    }

    public void Update()
    {
        timer += Globals.DT;
    }

    public void Draw()
    {
        if (size <= 0)
            return;

        float t = MathHelper.Clamp(timer / SECONDS, 0f, 1f);

        Texture2D fireball = BlastTexture.Get(size);

        // Grows the whole way through and fades over the back half, so the
        // flash is at its brightest the instant the damage was actually dealt
        float scale = MathHelper.Lerp(START_SCALE, END_SCALE, t);
        float alpha = 1f - t * t;

        Globals.SpriteBatch.Draw(
            fireball,
            new Vector2(Bounds.X, Bounds.Y),
            null,
            Color.Lerp(Color.White, colour, t) * alpha,
            0f,
            new Vector2(fireball.Width / 2f, fireball.Height / 2f),
            scale,
            SpriteEffects.None,
            0f
        );
    }
}
