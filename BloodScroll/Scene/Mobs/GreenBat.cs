using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE POISON BAT
//
// The ordinary bat's drawing washed green, and its shots wear the same colour.
// Same shape as the plain bat on purpose - the COLOUR is the only thing telling
// them apart in a swarm.
//
// Touching it or its shots poisons you: HP bleeds away and STOPS AT
// Player.POISON_FLOOR, so it can never kill on its own. What it does is take
// the shield out of the equation - poison goes straight to HP.
//
// Hits harder than a plain bat on contact, because the poison is the slow part.
//

public class GreenBat : BatBase
{
    // The plain bat's art - the tint is the whole difference
    protected override string AwakeRegion => "bat-animation";
    protected override string SleepingRegion => "bat-sleeping-animation";

    // Slower than the purple one and quicker than the plain one. It is meant to
    // be caught up with and shot down - a poison carrier you cannot reach is a
    // poison carrier that just poisons you.
    protected override int SpeedMin => 150;
    protected override int SpeedMax => 250;

    protected override int TargetOffset => 600;

    //
    // THE DOSE - one point a second, for twenty seconds.
    //
    // A single bite is almost nothing. THE DOSES STACK (see Player.ApplyPoison),
    // so four bats is four points a second, and the floor holds however many
    // land.
    //
    public const int POISON_DAMAGE = 20;
    public const float POISON_SECONDS = 20f;

    private static readonly BatShot Bullet = new(
        Region: "projectile-purple",   // the size of the circle, not its art
        Damage: 15,
        Speed: 750.0f,
        Aim: 120,
        Effect: BulletEffect.Poison,
        Colour: null);                 // left to the poison, which paints it green

    protected override BatShot Shot => Bullet;

    // Its own colour, and the stun blue still wins over it - see MobBase.Tinted
    protected override Color DrawColour => Tinted(Globals.PoisonGreen);

    // Green even before it wakes up, so a swarm can be read on the ceiling
    protected override Color SleepingColour => Globals.PoisonGreen;

    public GreenBat()
    {
        SetHP(60);
        DAMAGE = 30;
        PointsOnKill = 75;
        ON_TOUCH = OnTouch.PoisonPlayer;
    }
}
