using Microsoft.Xna.Framework;

namespace BloodScroll;

public interface IPlayer
{
    int HP { get; set; }
    int MaxHP { get; }
    float Bottom { get; }
    float Height { get; }
    float Width { get; }
    Vector2 Velocity { get; }
    Vector2 Position { get; }
    Vector2 PrevPos { get; }
    Rectangle Bounds { get; }
    void Heal();  
    void IncreaseMaxHP(int newMaxHP);
    void TakeDamage(int damage, IAudioService audio);
    void PlacePlayerOnPlatform(float platformY);
    void SetPlayerInAir(bool b);
    bool IsPlayerStandingOnPlatform(Rectangle platform);
}