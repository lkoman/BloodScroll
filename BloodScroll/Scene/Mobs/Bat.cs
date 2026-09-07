namespace BloodScroll;

//
// THE PLAIN BAT
//
// The basic chaser. No shot at all and the slowest of the three, so a room of
// only these can be out-run. Everything it does is in BatBase - this is just
// the numbers.
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
