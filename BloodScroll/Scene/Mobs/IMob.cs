using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace BloodScroll;

public interface IMob : IDrawableLayer
{
    Rectangle Bounds {get; set;}
    int HP {get; set; }
    int DAMAGE {get; set; }
    int PointsOnKill {get; set; }
    bool HittingPlayer {get; set;}
    void LoadContent(Vector2 playerPos, int spawnLayer);
    void Update(Vector2 playerPos, GameWorld gameWorld);
    void TakeDamage(int damage, IAudioService audio);
    void BounceFromFloor();
}