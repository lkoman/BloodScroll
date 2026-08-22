namespace BloodScroll;

//
// THE PLAIN BAT
//
// The basic chaser, and the yardstick every other bat is read against. It has
// no shot at all - it can only reach the player by arriving where he is - and it
// is the slowest of the three, so a room of nothing but these is a room you can
// out-run. Everything it does is in BatBase; all that is here is the numbers.
//

public class Bat : BatBase
{
    protected override string AwakeRegion => "bat-animation";
    protected override string SleepingRegion => "bat-sleeping-animation";

    protected override int SpeedMin => 100;
    protected override int SpeedMax => 200;

    // kok bat kiksne ko se zaleti v playerja. The widest of the three, so it
    // overshoots hardest and is the easiest to side-step.
    protected override int TargetOffset => 800;

    public Bat()
    {
        SetHP(50);
        DAMAGE = 25;
        PointsOnKill = 25;
    }
}
