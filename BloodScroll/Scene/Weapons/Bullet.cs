using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// BULLET
// Spawns, updates and draws a bullet
//

public class Bullet: IDrawableLayer
{
    public int DrawLayer { get; set; } = 35;

    public Sprite _bullet;
    public Circle bulletBounds;
    public int DAMAGE;
    private float SPEED;
    public Vector2 direction = Vector2.Zero;
    public int active = 1;
    public bool bounce = false;
    public Vector2 Velocity;

    public Bullet() {}

    public void LoadContent(Vector2 spawn, Vector2 target, string bulletType, int damage, float speed)
    {
        _bullet = new Sprite();
        _bullet = Globals.Weapons.CreateSprite(bulletType);

        DAMAGE = damage;
        SPEED = speed;

        _bullet.Position = spawn;

        direction = Vector2.Normalize(target - spawn);

        bulletBounds = CollisionManager.SetBoundingCircle(_bullet);
    }

    public void Update()
    {
        _bullet.Position = MovementUtils.MoveForward(_bullet.Position, direction, SPEED);

        // IF BULLET IS THREE SCREENS AWAY IN EVERY DIRECTION, IT DISSAPEARS
        float left   = -Globals.CameraOffset.X - Core.windowWidth  * 3;
        float right  = -Globals.CameraOffset.X + Core.windowWidth  * 3;
        float top    = -Globals.CameraOffset.Y - Core.windowHeight * 3;
        float bottom = -Globals.CameraOffset.Y + Core.windowHeight * 3;

        if (_bullet.Position.X < left ||
            _bullet.Position.X > right ||
            _bullet.Position.Y < top ||
            _bullet.Position.Y > bottom)
        {
            active = 0;
        }

        bulletBounds = CollisionManager.UpdateBoundingCircle(bulletBounds, _bullet);
    }

    public void Draw()
    {
        _bullet.Draw();
    }
}