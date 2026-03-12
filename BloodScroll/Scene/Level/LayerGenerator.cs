using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace BloodScroll;

//
// LIBRARY LAYER / PLATFORM GENERATOR
// - na layer 0 je en velik platform,
// - nato se v layer neskončno generirata dve poti (ena na levi strani ekrana, druga na desni)
// - vsak platform se generira randomly, toliko daleč, da player še lahko skoči nanjo iz prejšnje platforme
//

public class LayerGenerator()
{
    public static Sprite GetBackground(int currentLayerIndex, Vector2 layerOffset)
    {
        string backgroundName = "background6";
        if (currentLayerIndex <= 5)
        {
            backgroundName = "background" + currentLayerIndex;
        }

        Sprite _background = Globals.Backgrounds.CreateSprite(backgroundName);
        _background.Position -= layerOffset;

        return _background;
    }

    public static (List<Platform>, Vector2[]) GeneratePlatforms(int currentLayerIndex, Vector2 layerOffset, Vector2[] lastPlatformPos)
    {
        List<Platform> platforms = [];

        if (currentLayerIndex == 0)
            platforms = GenerateBigPlatforms(platforms, layerOffset);

        return GenerateSmallPlatforms(platforms, layerOffset, currentLayerIndex, lastPlatformPos);
    }

    private static (List<Platform>, Vector2[]) GenerateSmallPlatforms(List<Platform> platforms, Vector2 layerOffset, int currentLayerIndex, Vector2[] lastPlatformPos)
    {
        Platform smallPlatform = new();
        smallPlatform = smallPlatform.GenerateNewPlatform("small-platform", new Vector2(0, 0));

        Platform bigPlatform = new();
        bigPlatform = bigPlatform.GenerateNewPlatform("platform", new Vector2(0, 0));

        // GENERATE X PATHS
        for (int i = 0; i < lastPlatformPos.Length; i++)
        {
            (platforms, lastPlatformPos[i]) = GeneratePath(
                platforms,
                layerOffset,
                currentLayerIndex,
                lastPlatformPos[i],
                bigPlatform,
                smallPlatform, i);
        }

        return (platforms, lastPlatformPos);
    }

    private static (List<Platform>, Vector2) GeneratePath(List<Platform> platforms, Vector2 layerOffset, int currentLayerIndex, Vector2 lastPlatformPos, Platform bigPlatform, Platform smallPlatform, int sideOfScreen)
    {
        Vector2 platformPosition;
        Vector2 prevPlatformPos;

        // FIRST LAYER
        if (currentLayerIndex == 0)
        {
            platformPosition.Y = Core.windowHeight - bigPlatform.Height - Player.PLAYER_MAX_JUMP_Y;

            if (sideOfScreen == 0)
            {
                platformPosition.X = (float)(Globals.R.NextDouble() * (Core.windowWidth / 2 - 0) + 0);
            }
            else
            {
                platformPosition.X = (float)(Globals.R.NextDouble() * (Core.windowWidth - Core.windowWidth / 2) + Core.windowWidth / 2);
            }

            // Generate first platform in layer 0
            platforms.Add(new Platform());
            platforms.Last().GenerateNewPlatform("small-platform", platformPosition);

            prevPlatformPos = platforms.Last().position;
        }
        else prevPlatformPos = lastPlatformPos;

        // Generate all other platforms
        while(true)
        {
            platformPosition = SmallPlatformPosition(currentLayerIndex, prevPlatformPos, smallPlatform, sideOfScreen);

            if (platformPosition.Y < 0 - layerOffset.Y)
                break;

            platforms.Add(new Platform());

            platforms.Last().GenerateNewPlatform(
                "small-platform",
                platformPosition
            );

            platforms.Last().bounds.Y += (int)layerOffset.Y;
            platforms.Last().bounds.X += (int)layerOffset.X;

            prevPlatformPos = platforms.Last().position;
        }

        return (platforms, prevPlatformPos);
    }

    private static Vector2 SmallPlatformPosition(int layerIndex, Vector2 prevPlatform, Platform smallPlatform, int sideOfScreen)
    {   
        float playerHeight = 102;

        float max_x = prevPlatform.X + Player.PLAYER_MAX_JUMP_X;
        float min_x = prevPlatform.X - Player.PLAYER_MAX_JUMP_X;

        // LEVA STRAN EKRANA
        if (sideOfScreen == 0)
        {
            max_x = Math.Clamp(max_x, 
                0, 
                (Core.windowWidth + Globals.CameraOffset.X - smallPlatform.Width) / 2
            );
            min_x = Math.Clamp(min_x, 
                0, 
                (Core.windowWidth + Globals.CameraOffset.X - smallPlatform.Width) / 2
            );
        }

        // DESNA STRAN EKRANA
        else
        {
            max_x = Math.Clamp(max_x, 
                (Core.windowWidth + Globals.CameraOffset.X - smallPlatform.Width) / 2, 
                Core.windowWidth + Globals.CameraOffset.X - smallPlatform.Width
            );
            min_x = Math.Clamp(min_x, 
                (Core.windowWidth + Globals.CameraOffset.X - smallPlatform.Width) / 2, 
                Core.windowWidth + Globals.CameraOffset.X - smallPlatform.Width
            );
        }

        float max_y = prevPlatform.Y - Player.PLAYER_MAX_JUMP_Y;
        float min_y = prevPlatform.Y - playerHeight;

        //max_y = Math.Clamp(max_y, 0 - Globals.CameraOffset.Y + playerHeight, Core.windowHeight - Globals.CameraOffset.Y - smallPlatform.Height);
        //min_y = Math.Clamp(min_y, 0 - Globals.CameraOffset.Y + playerHeight, Core.windowHeight - Globals.CameraOffset.Y - smallPlatform.Height);
        
        float x = (float)(Globals.R.NextDouble() * (max_x - min_x) + min_x);
        float y = (float)(Globals.R.NextDouble() * (max_y - min_y) + min_y);

        Vector2 Pos = new(x, y);

        return Pos;
    }

    private static List<Platform> GenerateBigPlatforms(List<Platform> platforms, Vector2 layerOffset)
    {
        Platform bigPlatform = new();
        bigPlatform = bigPlatform.GenerateNewPlatform("platform", new Vector2(0, 0));

        int numBigPlatforms = (int)Math.Ceiling(Core.windowWidth / bigPlatform.Width);

        // Create a row of numOfPlatforms number of platforms, every second one flipped horizontally
        for (int i = 0; i < numBigPlatforms; i++)
        {
            platforms.Add(new Platform());
            platforms.Last().GenerateNewPlatform("platform", new Vector2(bigPlatform.Width * i, Core.windowHeight - bigPlatform.Height) - layerOffset);
            platforms.Last().bounds.Y -= (int)layerOffset.Y;
            platforms.Last().bounds.X -= (int)layerOffset.X;

            if (i % 2 != 0)
            {
                platforms.Last().SetPlatformEffects(SpriteEffects.FlipHorizontally);
            }
        }

        return platforms;
    }
}