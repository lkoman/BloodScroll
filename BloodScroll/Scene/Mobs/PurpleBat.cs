using MonoGameLibrary;

namespace BloodScroll;

//
// THE PURPLE BAT
//
// The plain bat's flight with a gun bolted on: it fires every time it picks a
// new heading, so the shot always arrives from wherever it has just turned. The
// fastest of the three and the frailest - half the plain bat's HP - because a
// mob that shoots at you from across the room has to be killable from there too.
//

public class PurpleBat : BatBase
{
    protected override string AwakeRegion => "purple-bat-animation";
    protected override string SleepingRegion => "purple-bat-sleeping-animation";

    protected override int SpeedMin => 200;
    protected override int SpeedMax => 300;

    // kok bat kiksne ko se zaleti v playerja
    protected override int TargetOffset => 600;

    // kok bat kiksne ko strela v playerja. Much tighter than the heading above:
    // the flying is meant to be sloppy, the shooting is not.
    private static readonly BatShot Bullet = new(
        Region: "projectile-purple",    // the size of the circle, not its art
        Damage: 25,
        Speed: 800.0f,
        Aim: 100,
        Colour: Globals.BatPurple);     // the same purple it is drawn in

    protected override BatShot Shot => Bullet;

    public PurpleBat()
    {
        SetHP(25);
        DAMAGE = 25;
        PointsOnKill = 50;
    }
}
