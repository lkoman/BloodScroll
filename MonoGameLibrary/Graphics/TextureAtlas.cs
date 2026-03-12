using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MonoGameLibrary.Graphics;

//
// Dictonary texture-ov - dobi jih iz xml
//
// Funkcije createSprite, createAnimatedSprite
//
public class TextureAtlas 
{
    private Dictionary<string, TextureRegion> _regions;
    public Texture2D Texture { get; set; }
    private Dictionary<string, Animation> _animations; // Stores animations added to this atlas.

    public TextureAtlas()
    {
        _regions = [];
        _animations = [];
    }

    public TextureAtlas(Texture2D texture)
    {
        Texture = texture;
        _regions = [];
        _animations = [];
    }

    public static TextureAtlas FromFile(ContentManager content, string fileName)
    {
        TextureAtlas atlas = new();

        string filePath = Path.Combine(content.RootDirectory, fileName);

        using Stream stream = TitleContainer.OpenStream(filePath);
        using XmlReader reader = XmlReader.Create(stream);

        XDocument doc = XDocument.Load(reader);
        XElement root = doc.Root;

        string texturePath = root.Element("Texture").Value;
        atlas.Texture = content.Load<Texture2D>(texturePath);

        // Example:
        // <Regions>
        //      <Region name="spriteOne" x="0" y="0" width="32" height="32" />
        //      <Region name="spriteTwo" x="32" y="0" width="32" height="32" />
        // </Regions>
        var regions = root.Element("Regions")?.Elements("SubTexture");

        if (regions != null)
        {
            foreach (var region in regions)
            {
                string name = region.Attribute("name")?.Value;
                int x = int.Parse(region.Attribute("x")?.Value ?? "0");
                int y = int.Parse(region.Attribute("y")?.Value ?? "0");
                int width = int.Parse(region.Attribute("width")?.Value ?? "0");
                int height = int.Parse(region.Attribute("height")?.Value ?? "0");

                if (!string.IsNullOrEmpty(name))
                {
                    atlas.AddRegion(name, x, y, width, height);
                }
            }
        }

        // Example:
        // <Animations>
        //      <Animation name="animation" delay="100">
        //          <Frame region="spriteOne" />
        //          <Frame region="spriteTwo" />
        //      </Animation>
        // </Animations>
        var animationElements = root.Element("Animations").Elements("Animation");

        if (animationElements != null)
        {
            foreach (var animationElement in animationElements)
            {
                string name = animationElement.Attribute("name")?.Value;
                float delayInMilliseconds = float.Parse(animationElement.Attribute("delay")?.Value ?? "0");
                TimeSpan delay = TimeSpan.FromMilliseconds(delayInMilliseconds);

                List<TextureRegion> frames = [];

                var frameElements = animationElement.Elements("Frame");

                if (frameElements != null)
                {
                    foreach (var frameElement in frameElements)
                    {
                        string regionName = frameElement.Attribute("region").Value;
                        TextureRegion region = atlas.GetRegion(regionName);
                        frames.Add(region);
                    }
                }

                Animation animation = new(frames, delay);
                atlas.AddAnimation(name, animation);
            }
        }

        return atlas;
    }

    // REGION HELPER FUNCTIONS
    public void AddRegion(string name, int x, int y, int width, int height)
    {
        TextureRegion region = new TextureRegion(Texture, x, y, width, height);
        _regions.Add(name, region);
    }

    public TextureRegion GetRegion(string name)
    {
        return _regions[name];
    }

    public bool RemoveRegion(string name)
    {
        return _regions.Remove(name);
    }

    public void Clear()
    {
        _regions.Clear();
    }

    // SPRITE HELPER FUNCTIONS
    public Sprite CreateSprite(string regionName)
    {
        TextureRegion region = GetRegion(regionName);
        return new Sprite(region);
    }

    // ANIMATION HELPER FUNCTIONS
    public void AddAnimation(string animationName, Animation animation)
    {
        _animations.Add(animationName, animation);
    }

    public Animation GetAnimation(string animationName)
    {
        return _animations[animationName];
    }

    public bool RemoveAnimation(string animationName)
    {
        return _animations.Remove(animationName);
    }

    // ANIMATED SPRITE HELPER FUNCTIONS
    public AnimatedSprite CreateAnimatedSprite(string animationName)
    {
        Animation animation = GetAnimation(animationName);
        return new AnimatedSprite(animation);
    }

}
