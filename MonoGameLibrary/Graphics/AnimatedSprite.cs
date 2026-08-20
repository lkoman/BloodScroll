using System;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary.Graphics;

//
// Runs the animation v svojem UPDATE()
//

public class AnimatedSprite : Sprite 
{
    private int _currentFrame;
    public int CurrentFrame => _currentFrame;
    public int FramesCount => _animation.Frames.Count;
    private TimeSpan _elapsed;
    private Animation _animation;
    public Animation Animation
    {
        get => _animation;
        set
        {
            _animation = value;
            Region = _animation.Frames[0];
        }
    }

    public AnimatedSprite() { }

    public AnimatedSprite(Animation animation)
    {
        Animation = animation;
    }

    public void Update()
    {
        _elapsed += Globals.ElapsedTime;

        if (_elapsed >= _animation.Delay)
        {
            _elapsed -= _animation.Delay;
            _currentFrame++;

            if (_currentFrame >= _animation.Frames.Count)
            {
                _currentFrame = 0;
            }

            Region = _animation.Frames[_currentFrame];
        }
    }
}
