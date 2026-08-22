using Microsoft.Xna.Framework;

namespace BloodScroll;

//
// THE BLACK SPIDER
//
// The ceiling patrol again, drawn almost black, spitting webs that are almost
// black. Everything about it is the ordinary spider except the one thing that
// matters: its web does not slow you down, IT NAILS YOU TO THE SPOT.
//
// A second is not long. It is long enough for the three bats you were running
// from to arrive, which is the whole idea - it does no damage itself, it just
// stops being your turn while everything else on the layer takes theirs.
//
// IT IS RARE ON PURPOSE (see WaveData: two at most, and none before layer ten).
// Losing the controls is the harshest thing the game does to the player, and it
// stays frightening exactly as long as it stays uncommon. A layer with four of
// these on the ceiling would not be harder, it would be unplayable - the player
// would spend the fight standing still watching himself be eaten.
//
// It is also slower to shoot and slower to walk than the ordinary spider, so
// there is time to see one, work out which spider it is, and kill it first.
//

public class BlackSpider : CeilingSpider
{
    protected override float PatrolSpeed => 260f;
    protected override float ShootSecondsMin => 3.5f;
    protected override float ShootSecondsMax => 5.5f;

    // Slower than an ordinary web as well, so a black shot coming down is a
    // shot the player has a real chance to step out of
    protected override float WebSpeed => 520.0f;

    // Not a stronger slow - a different thing entirely. It takes the controls
    // away instead of taxing them.
    protected override BulletEffect WebEffect => BulletEffect.Root;

    // How long the player is stuck. One second, and not a frame more - this is
    // the number that decides whether the mob is tense or unfair.
    public const float ROOT_SECONDS = 1f;

    // THE BODY IS DARKER THAN THE ORDINARY SPIDER BUT NOT AS DARK AS ITS WEB.
    // The web is meant to be missed until it is on top of you; the spider is
    // not - the player has to be able to pick it out of the ceiling and choose
    // to shoot that one first, which is the entire counterplay to it.
    private static readonly Color BODY = new(74, 64, 92);

    // Its own colour, with the stun blue still winning - see MobBase.Tinted
    protected override Color DrawColour => Tinted(BODY);

    public BlackSpider()
    {
        // Tougher than the ordinary one. It should be the spider you deal with
        // first, and it should cost something to do that.
        SetHP(110);
        DAMAGE = 25;
        PointsOnKill = 150;
    }
}
