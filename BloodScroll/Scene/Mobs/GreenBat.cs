using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE POISON BAT
//
// The ordinary bat's drawing, washed a sickly green, and its shots wear the
// same colour. Wearing the same shape is the point: it flies in the same swarm
// as the plain bats and you have to pick it out of them, and the ONE thing that
// tells you which is which is the colour of it.
//
// WHAT IT ACTUALLY DOES
//
// Touching it or being hit by one of its shots poisons you: HP bleeds away for
// ten seconds and STOPS AT TEN. It cannot kill - see Player.UpdatePoison for
// why - so on its own it is never fatal. What it does is take the shield out
// of the equation (poison goes straight to HP) and hand every other mob in the
// room a much easier target for the next ten seconds. It is a mob that makes
// the fight around it worse rather than one that beats you itself.
//
// It hits harder than a plain bat on contact for the same reason: the poison is
// the slow part, and something has to make walking into one hurt right away.
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
    // THE DOSE
    //
    // ONE POINT A SECOND, FOR TWENTY SECONDS.
    //
    // A single bite is almost nothing - twenty HP spread so thin the player can
    // out-heal it and mostly ignore it. That is deliberate: what costs you is
    // not one bat, it is FOUR, because the doses stack (see Player.ApplyPoison)
    // and four of them is four points a second for as long as they are on you.
    //
    // So the green bat has stopped being a mob that punishes one mistake and
    // become one that punishes letting them pile up, which is the thing the
    // swarm was always about. And none of it can kill on its own - the floor at
    // ten HP holds however many bites land.
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
