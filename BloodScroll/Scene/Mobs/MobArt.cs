using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// WHERE EVERY MOB GETS ITS SPRITES
//
// The mobs below are finished and playable, but they are still drawing with
// borrowed art. Everything marked PLACEHOLDER points at an existing region so
// the behaviour could be built and tested before the real sprites exist.
//
// TO SWAP IN YOUR OWN DRAWINGS:
//   1. put the PNG in Content/images/ and write the matching atlas XML
//      (<Texture>, one <SubTexture> per frame, one <Animation> per state -
//       the <Animations> element must exist even if it is empty)
//   2. add two blocks to Content/Content.mgcb: /build: the PNG, /copy: the XML
//   3. if it is a new atlas file, add a Globals.<Name> field and a
//      TextureAtlas.FromFile line in BloodScroll.LoadContent
//   4. change the constant here, and point the mob at the lookup for that
//      atlas at the bottom of this file - nothing else needs to change
//
// Sizes that fit the existing art: flower <=128 wide (a platform is 128px),
// butterfly 96x96 idle / ~192x192 burst, bosses 512x512 (what the fire boss,
// the spider queen and the moth all are).
//

public static class MobArt
{
    // FLOWER - sleeps on a platform until you come near, then bites
    //
    // Drawn in THREE PARTS, because the stalk is a spring and not a picture:
    //   the head    is the mob - it is what gets shot and what bites
    //   the leaves  are the fixed root it is tied to
    //   the stem    is ONE short piece, repeated from the leaves to the head
    //               as many times as the stalk is currently long. Draw it
    //               upright and narrow, growing upwards, with the end that
    //               joins the piece below at the BOTTOM of the frame.
    //
    // All four are their own animation in flower.xml, so extra frames are
    // added there and nothing in the code changes. The ones marked PLACEHOLDER
    // are still pointing at the idle head because that drawing does not exist
    // yet - draw it, give it an Animation of its own, and repoint the constant.
    public const string FlowerSleeping = "flower-sleeping";     // PLACEHOLDER - closed up, asleep
    public const string FlowerIdle = "flower-idle";             // awake, swaying
    public const string FlowerBite = "flower-bite";             // PLACEHOLDER - jaws open, mid lunge
    public const string FlowerLeaves = "flower-leaves";         // the root, never moves
    public const string FlowerStem = "flower-stem";             // one repeated piece

    // SPIDER - patrols the ceiling and spits webs.
    // Drawn side on facing LEFT, which is the opposite of what the movement
    // helper assumes, so Spider.cs flips it back. Redraw it facing right and
    // that flip is what you delete.
    public const string Spider = "spider";                      // spiders atlas

    // BUTTERFLY - walk into it, wait out the fuse, stand in the burst to heal
    //
    // Real art of its own now, in its own warm yellows. It used to be the
    // jellyfish drawing washed green, because the two mobs shared a shape and
    // the colour was the only thing telling "heals you" from "kills you" - with
    // a drawing that is plainly a butterfly and plainly not a jellyfish, that
    // tint is not needed any more and would only mud up the artwork. Which is
    // why Butterfly.cs draws it white: the art carries its own meaning.
    public const string Butterfly = "butterfly-animation";
    public const string ButterflyBurst = "butterfly-explode-animation";

    // SPIDER QUEEN
    // She is pinned to the middle of her own body and TURNS to look at the
    // player instead of flipping, so all three of these have to be drawn
    // the same way: body centred in the frame, face pointing the same
    // direction. Tell the code which direction that is with ART_FACING at the
    // top of SpiderQueen.cs.
    // The art is top down with her face pointing UP, so ART_FACING there is
    // already set to match. There is only the one animation so far - give the
    // aim and the ram their own the moment they are drawn, because the turn
    // before a ram is the only warning the player gets.
    public const string SpiderQueen = "spiderBoss";      // spiders atlas - walking
    public const string SpiderQueenAim = "spiderBoss";   // spiders atlas - turning to face you
    public const string SpiderQueenLunge = "spiderBoss"; // spiders atlas - the ram

    // MOTH AND ITS COCOON
    // Like the spider queen, the moth is pinned to the middle of its own body
    // and TURNS to look at the player instead of flipping, so both moth frames
    // have to be drawn the same way round: body centred, head pointing the same
    // direction. The art is top down with the head UP, and ART_FACING at the
    // top of Moth.cs is already set to match.
    //
    // MothAttack is the WING BEAT it does standing still, right before a gust
    // goes out - not a flying pose. Moth is used for everything else, the ram
    // included.
    //
    // The gust itself has no art: it is two curved white lines baked in
    // MothGust.cs, sized off whatever the moth turns out to be.
    public const string Moth = "MothBoss_idle";                   // flying
    public const string MothAttack = "MothBoss_attack";           // wings beating
    public const string CocoonClosed = "MothBoss_cocoon_closed";  // moth inside, healing
    public const string CocoonOpen = "MothBoss_cocoon_open";      // empty

    // THE HOLE deliberately has no art either - it is a black hole, baked pixel
    // by pixel in Hive.cs and turned instead of animated. Nothing to draw.

    // WEBS. Every web in the game - the patch you stand in, the shot a spider
    // spits and the one wrapped round the player - is the baked orb web from
    // WebTexture, so there is no region for any of them.
    //
    // The projectile still names one, because a bullet hangs its hitbox on a
    // sprite. It is the carcass under the web, never what gets drawn.
    public const string WebProjectile = "projectile-purple";    // PLACEHOLDER (from the weapons atlas)

    // One lookup per atlas. Most placeholder art comes out of the enemies
    // atlas - the butterfly is the exception, it borrows the jellyfish - and a
    // mob with real art of its own calls the lookup for the file it lives in.
    public static AnimatedSprite Enemy(string region) => Globals.Enemies.CreateAnimatedSprite(region);
    public static AnimatedSprite Spiders(string region) => Globals.Spiders.CreateAnimatedSprite(region);
    public static AnimatedSprite Flower(string region) => Globals.Flower.CreateAnimatedSprite(region);
    public static AnimatedSprite Jellyfish(string region) => Globals.Jellyfish.CreateAnimatedSprite(region);
    public static AnimatedSprite Butterflies(string region) => Globals.Butterfly.CreateAnimatedSprite(region);
    public static AnimatedSprite MothBoss(string region) => Globals.MothBoss.CreateAnimatedSprite(region);

    // SHADOW TWIN deliberately has no art of its own - it is the player,
    // drawn in near black. Nothing to draw for this one.
    public static AnimatedSprite PlayerLookalike() => Globals.Player.CreateAnimatedSprite("player-idle");
}
