using System.Collections.Generic;

namespace BloodScroll;

//
// A mob that lives on a platform instead of spawning into open air.
// MobManager places these when the layer is built, once the platforms exist.
//

public interface IPlatformMob : IMob
{
    // `platform` is the one it stands on. `layerPlatforms` is every platform
    // on the layer, so a mob whose range is measured in LEDGES rather than in
    // pixels can work out where its neighbours actually are - platforms are
    // placed by jump distance, so that gap is different every time.
    void PlaceOnPlatform(Platform platform, IReadOnlyList<Platform> layerPlatforms);
}
