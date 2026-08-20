using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

using Vector2 = Microsoft.Xna.Framework.Vector2;

//
// PLAYER - LOAD, UPDATE, DRAW
// HANDLE INPUT
//

public class Player : IPlayer, IDrawableLayer
{
    public int DrawLayer { get; set; } = 30;

    // INTERFACE VARIABLES
    public int HP 
    { 
        get => playerHP; 
        set => playerHP = value; 
    }
    public int MaxHP => playerMaxHP;
    public Rectangle Bounds => playerBounds;
    public Vector2 Position => _player.Position;
    public Vector2 PrevPos => prevPos;
    public float Bottom => _player.Bottom;
    public float Height => _player.Height;
    public float Width => _player.Width;
    public Vector2 Velocity => velocity;

    // MAX JUMP
    public static readonly float PLAYER_MAX_JUMP_Y;
    public static readonly float PLAYER_MAX_JUMP_X;
    public static readonly float PLAYER_MAX_JUMP_X_SPRINT;

    private AnimatedSprite _player;
    private AnimatedSprite _player_idle, _player_running, _player_in_jump;
    private Rectangle playerBounds;
    public Rectangle PlayerBounds => playerBounds;
    private Vector2 prevPos;
    private readonly float playerStartHeight = Core.windowHeight - 300;

    private const int StartPlayerHP = 500;
    private int playerMaxHP = StartPlayerHP; // Max HP of player (player gets this amount of HP when beating a boss)
    private int playerHP = StartPlayerHP; // Current player HP
    
    // PLAYER MOVEMENT
    private const float SPEED = 100.0f;
    private const float MAX_SPEED = 600.0f;
    private const float JUMP = 800.0f; 
    private const float GRAVITY = 9.81f * 200;
    private const float SPRINT_ACCEL = 1.5f;

    private float speed, max_speed, jump = 0f;
    private bool canJump = true;
    private Vector2 velocity = new (0f, 0f);
    public bool inAir = true;

    // HIT TIMER
    private bool isHit = false;
    private float hitTimer = 0f;
    private const float hitDuration = 0.15f; // seconds

    // Weapons
    private bool canSwitchWeapon = true;

    static Player()
    {
        // PLAYER_MAX_JUMP_Y
        float dt = 1f / 60f;

        int N = (int)Math.Ceiling(JUMP / (GRAVITY * dt));
        PLAYER_MAX_JUMP_Y = (float)(-dt * ( N * (-JUMP) + GRAVITY * dt * N * (N + 1) / 2.0 ));

        float time_up = JUMP / GRAVITY;
        float jump_time = time_up * 2f;
        PLAYER_MAX_JUMP_X = MAX_SPEED * jump_time;
        PLAYER_MAX_JUMP_X_SPRINT = PLAYER_MAX_JUMP_X * SPRINT_ACCEL;
    }

    public void Restart()
    {
        playerMaxHP = StartPlayerHP;
        playerHP = StartPlayerHP;

        _player = _player_idle;
        _player.Position = new Vector2(Core.windowWidth / 2 - _player.Width / 2, playerStartHeight);
        playerBounds = CollisionManager.SetBoundingRectangle(_player);

        speed = 0f; max_speed = 0f; jump = 0f;
        canJump = true;
        velocity = new (0f, 0f);
        SetPlayerInAir(true);
    }

    public void LoadContent()
    {
        _player_idle = Globals.Player.CreateAnimatedSprite("player-idle");
        _player_running = Globals.Player.CreateAnimatedSprite("player-running");
        _player_in_jump = Globals.Player.CreateAnimatedSprite("player-in-jump");

        _player = _player_idle;
        _player.Position = new Vector2(Core.windowWidth / 2 - _player.Width / 2, playerStartHeight);
        playerBounds = CollisionManager.SetBoundingRectangle(_player);
    }

    public void Update(IAudioService audio, WeaponsManager weaponsManager)
    {
        if (Globals.PLAYER_ALIVE)
            CheckKeyboardInput(audio, weaponsManager);
        else {
            velocity.X = 0;
            velocity.Y = 0;
        }

        UpdateHitTimer();
        
        prevPos = _player.Position;
        _player.Position += velocity * Globals.DT;

        if (PlayerHitEdge()) {
            // Player fall down the edge
            _player.Position = prevPos;
            velocity.X = 0;
            if (inAir)
                velocity.Y += GRAVITY * Globals.DT;

            _player.Position += velocity * Globals.DT;
        }

        playerBounds = CollisionManager.UpdateBoundingRectangle(playerBounds, _player);

        _player.Update();

        if (playerHP <= 0) {
            Globals.PLAYER_ALIVE = false;
        }
    }

    public void Draw()
    {
        if (inAir) {
            _player_in_jump.Position = _player.Position;
            _player = _player_in_jump;
        }
        else if (velocity.X != 0) {
            _player_running.Position = _player.Position;
            _player = _player_running;
        }
        else {
            _player_idle.Position = _player.Position;
            _player = _player_idle;
        }

        Color drawColor = isHit ? Globals.Red : Color.White;
        _player.Draw(drawColor);
    }

    private void CheckKeyboardInput(IAudioService audio, WeaponsManager weaponsManager)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        speed = SPEED;
        max_speed = MAX_SPEED;
        jump = JUMP;

        // WEAPON SWITCH
        if (keyboardState.IsKeyDown(Keys.W) && canSwitchWeapon)
        {
            weaponsManager.SwitchWeapon();
            canSwitchWeapon = false;
        }
        else if (keyboardState.IsKeyUp(Keys.W))
        {
            canSwitchWeapon = true;
        }

        // SPRINT
        if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
        {
            speed *= SPRINT_ACCEL;
            max_speed *= SPRINT_ACCEL;
        }

        // LEVO
        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
        {
            _player.Effects = SpriteEffects.FlipHorizontally;
            velocity.X -= speed;
        }
        // DESNO
        else if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
        {
            _player.Effects = SpriteEffects.None;
            velocity.X += speed;
        }
        // PREVENT SLIDE
        else velocity.X = 0;

        // JUMP
        if (keyboardState.IsKeyDown(Keys.Space) && canJump && !inAir)
        {
            audio.PlaySound(AudioId.PlayerJump);

            velocity.Y -= jump;
            SetPlayerInAir(true);
            canJump = false;
        }
        if (keyboardState.IsKeyUp(Keys.Space))
        {
            canJump = true;
        }
        if (inAir)
        {
            velocity.Y += GRAVITY * Globals.DT;
        }

        velocity.X = MyMath.Clamp(velocity.X, -max_speed, max_speed);
    }

    // Hit for changing color when player is hit
    private void UpdateHitTimer()
    {
        if (isHit)
        {
            hitTimer -= Globals.DT;
            if (hitTimer <= 0f)
                isHit = false;
        }
    }

    private bool PlayerHitEdge()
    {
        if (_player.Position.X > Core.windowWidth - _player.Width + Globals.CameraOffset.X ||
            _player.Position.X < 0 + Globals.CameraOffset.X)
        {
            return true;
        }
        return false;
    }

    public void Heal()
    {
        playerHP = playerMaxHP;
    }

    public void IncreaseMaxHP(int newMaxHP)
    {
        playerMaxHP = newMaxHP;
    }

    public void TakeDamage(int damage, IAudioService audio)
    {
        audio.PlaySound(AudioId.PlayerHit);

        isHit = true;
        hitTimer = hitDuration;
        
        playerHP -= damage;
        if (playerHP <= 0) playerHP = 0;
    }

    public void PlacePlayerOnPlatform(float platformY)
    {   
        _player.Position = new Vector2(_player.Position.X, platformY - _player.Height);
        velocity.Y = 0;
        SetPlayerInAir(false);
    }

    public void SetPlayerInAir(bool b)
    {
        inAir = b;
    }

    public bool IsPlayerStandingOnPlatform(Rectangle platform)
    {
        return 
            velocity.Y > 0 && // when velocity in Y is bigger then 0, the player is falling
            // Is players bottom on platform
            prevPos.Y + _player.Height <= platform.Y + platform.Height &&
            prevPos.Y + _player.Height >= platform.Y - platform.Height;
    }
}