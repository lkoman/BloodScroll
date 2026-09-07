using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE GREEN SPIDER
//
// Ceiling mob, drawn in fluorescent green and spitting webs.
// Same as normal spider, ampak da te ne upočasni, ampak prilepi na tla.
//
// IT IS VERY RARE (see WaveData: two at most, and none before layer ten).
//
// It is also slower to shoot and slower to walk than the ordinary spider.
//

public class GreenSpider : CeilingSpider
{
    protected override float PatrolSpeed => 260f;
    protected override float ShootSecondsMin => 3.5f;
    protected override float ShootSecondsMax => 5.5f;

    // Slower web than the ordinary spider, so it can be dodged
    protected override float WebSpeed => 520.0f;
    protected override BulletEffect WebEffect => BulletEffect.Root; // prilepi na tla
    public const float ROOT_SECONDS = 1f;

    // Same fluorescent green as its web, so it can be picked out of a ceiling
    // full of ordinary spiders. See Globals.RootWeb.
    private static readonly Color BODY = Globals.RootWeb;

    // Stun blue still wins - see MobBase.Tinted
    protected override Color DrawColour => Tinted(BODY);

    public GreenSpider()
    {
        // Tougher than the ordinary one - it should be killed first
        SetHP(110);
        DAMAGE = 25;
        PointsOnKill = 150;
    }
}
