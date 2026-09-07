namespace BloodScroll;

//
// THE ORDINARY SPIDER
//
// The ceiling patrol at its plainest. Its web does no damage at all - it just
// slows you down. See CeilingSpider for everything it actually does.
//

public class Spider : CeilingSpider
{
    protected override float PatrolSpeed => 350f;
    protected override float ShootSecondsMin => 2f;
    protected override float ShootSecondsMax => 3.5f;
    protected override float WebSpeed => 650.0f;

    // A tax on your movement, which you play through
    protected override BulletEffect WebEffect => BulletEffect.Slow;

    public Spider()
    {
        SetHP(60);
        DAMAGE = 20;
        PointsOnKill = 60;
    }
}
