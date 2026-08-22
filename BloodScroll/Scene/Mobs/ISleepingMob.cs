using Microsoft.Xna.Framework;

namespace BloodScroll;

//
// A mob that is created together with the whole layer but stays dormant,
// and then gets woken one wave at a time (the bats hanging from the ceiling).
//

public interface ISleepingMob : IMob
{
    void WakeUp(Vector2 playerPos);
}
