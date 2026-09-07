using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// WHERE EVERY MOB GETS ITS SPRITES
//
// Everything marked PLACEHOLDER points at borrowed art.
//
// TO SWAP IN YOUR OWN DRAWINGS:
//   1. PNG in Content/images/ + matching atlas XML (<Texture>, one <SubTexture>
//      per frame, one <Animation> per state - <Animations> must exist even if
//      it is empty)
//   2. two blocks in Content/Content.mgcb: /build: the PNG, /copy: the XML
//   3. new atlas file -> add a Globals.<Name> field and a TextureAtlas.FromFile
//      line in BloodScroll.LoadContent
//   4. change the constant here and point the mob at that atlas lookup below
//
// Sizes that fit: flower <=128 wide (a platform is 128px), butterfly 96x96 idle
// / ~192x192 burst, bosses 512x512.
//

public static class MobArt
{
    // FLOWER - sleeps on a platform until you come near, then bites.
    //
    // THREE PARTS, because the stalk is a spring not a picture:
    //   head    the mob - what gets shot and what bites
    //   leaves  the fixed root
    //   stem    ONE short piece, repeated from leaves to head. Draw it upright
    //           and narrow, growing upwards, joining end at the BOTTOM.
    //
    // Each is its own animation in flower.xml.
    public const string FlowerSleeping = "flower-sleeping";     // PLACEHOLDER - closed up, asleep
    public const string FlowerIdle = "flower-idle";             // awake, swaying
    public const string FlowerBite = "flower-bite";             // PLACEHOLDER - jaws open, mid lunge
    public const string FlowerLeaves = "flower-leaves";         // the root, never moves
    public const string FlowerStem = "flower-stem";             // one repeated piece

    // SPIDER - patrols the ceiling and spits webs.
    // Drawn side on facing LEFT, the opposite of what the movement helper
    // assumes, so CeilingSpider flips it back.
    public const string Spider = "spider";                      // spiders atlas

    // BUTTERFLY - walk into it, wait out the fuse, stand in the burst to heal.
    // Its own art, so Butterfly.cs draws it white with no tint.
    public const string Butterfly = "butterfly-animation";
    public const string ButterflyBurst = "butterfly-explode-animation";

    // SPIDER QUEEN. She TURNS instead of flipping, so all three must be drawn
    // the same way: body centred in the frame, face pointing the same direction.
    // Set ART_FACING at the top of SpiderQueen.cs to match (currently UP).
    // Only one animation so far - aim and ram still point at the walk frames.
    public const string SpiderQueen = "spiderBoss";      // spiders atlas - walking
    public const string SpiderQueenAim = "spiderBoss";   // spiders atlas - turning to face you
    public const string SpiderQueenLunge = "spiderBoss"; // spiders atlas - the ram

    // MOTH AND ITS COCOON. Same as the queen - it TURNS, so both moth frames
    // must be body centred with the head pointing the same way. ART_FACING at
    // the top of Moth.cs matches the art (head UP).
    //
    // MothAttack is the WING BEAT before a gust, not a flying pose. Moth is used
    // for everything else including the ram.
    //
    // The gust has no art - see MothGust.cs.
    public const string Moth = "MothBoss_idle";                   // flying
    public const string MothAttack = "MothBoss_attack";           // wings beating
    public const string CocoonClosed = "MothBoss_cocoon_closed";  // moth inside, healing
    public const string CocoonOpen = "MothBoss_cocoon_open";      // empty

    // THE HOLE has no art - baked pixel by pixel in Hive.cs and turned rather
    // than animated.

    // WEBS. Every web in the game is the baked orb web from WebTexture, so there
    // is no region for any of them. The projectile still names one only because
    // a bullet hangs its hitbox on a sprite - it is never drawn.
    public const string WebProjectile = "projectile-purple";    // PLACEHOLDER (from the weapons atlas)

    // One lookup per atlas
    public static AnimatedSprite Enemy(string region) => Globals.Enemies.CreateAnimatedSprite(region);
    public static AnimatedSprite Spiders(string region) => Globals.Spiders.CreateAnimatedSprite(region);
    public static AnimatedSprite Flower(string region) => Globals.Flower.CreateAnimatedSprite(region);
    public static AnimatedSprite Jellyfish(string region) => Globals.Jellyfish.CreateAnimatedSprite(region);
    public static AnimatedSprite Butterflies(string region) => Globals.Butterfly.CreateAnimatedSprite(region);
    public static AnimatedSprite MothBoss(string region) => Globals.MothBoss.CreateAnimatedSprite(region);

    // SHADOW TWIN has no art - it is the player sprite, drawn in near black
    public static AnimatedSprite PlayerLookalike() => Globals.Player.CreateAnimatedSprite("player-idle");
}
