using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;


namespace BloodScroll;

//
// GENERATE, UPDATE, DRAW LAYER
// - background
// - platforms
// - mob waves (so vezani na layer - vsak layer ima x mob waves)
//

public class Layer : IDrawableLayer
{
    public int DrawLayer { get; set; } = 0;
    
    private Vector2 layerOffset;
    public int layerIndex;
    public bool LAYER_BEATEN = false;

    // BACKGROUND
    private Sprite _background;

    // PLATFORMS on this layer
    private List<Platform> platforms = [];
    public IReadOnlyList<Platform> Platforms => platforms;

    // MOBS on this layer
    private MobManager mobManager;
    public MobManager MobManager => mobManager;

    // A layer keeps running until its waves are spent AND everything on it is
    // dead, so mobs can chase the player up through layers he ran past.
    //
    // NOT the same question as LayerFinished below: a boss layer is beaten when
    // its boss drops, but the escort is still alive and still being simulated.
    public bool NeedsUpdate =>
        mobManager.wavesTriggered &&
        !(mobManager.AllEnemiesBeaten && mobManager.waveData.WavesToBeat <= 0);

    //
    // WHAT COUNTS AS FINISHING THIS LAYER
    //
    // A BOSS LAYER IS OVER WHEN ITS BOSS IS - the escort can go on flying about.
    // Every other layer is over when its waves are all sent and everything from
    // them is dead.
    //
    private bool LayerFinished =>
        mobManager.waveData.LayerType == LayerType.Boss
            ? mobManager.BossBeaten
            : mobManager.waveData.WavesToBeat <= 0 && mobManager.AllEnemiesBeaten;


    // lastPlatformPosition is the position of the last generated platform of the previous layer,
        // to start the generation of this layer (for continuous platform generation)
    public Vector2[] GenerateLayer(int currentLayerIndex, Vector2[] lastPlatformPos, WaveData waveSettings)
    {
        layerIndex = currentLayerIndex;
        layerOffset = new Vector2(0f, currentLayerIndex * Core.windowHeight);

        // GENERATE LAYER
        _background = LayerGenerator.GetBackground(currentLayerIndex, layerOffset);
        (platforms, lastPlatformPos) = LayerGenerator.GeneratePlatforms(currentLayerIndex, lastPlatformPos);

        // SET MOB MANAGER FOR THIS LAYER
        mobManager = new(layerIndex, waveSettings);

        // Scenery first - it is part of the room, not part of a wave, and it is
        // hanging there before the player ever sets foot on the layer
        mobManager.GenerateSceneryMobs(layerIndex);

        // All dormant mobs need to be generated first, then we wake them up wave by wave
        mobManager.GenerateAllSleepingMobs(layerIndex);

        // Mobs that hide on the terrain, now that the platforms exist
        mobManager.GeneratePlatformMobs(platforms, layerIndex);

        return lastPlatformPos;
    }

    public void Update(IPlayer player, IWeaponsManager weaponsManager, IUI ui, GameWorld gameWorld, IAudioService audio)
    {
        if (!mobManager.wavesTriggered)
            return;

        mobManager.Update(player, layerIndex, gameWorld);

        if (LAYER_BEATEN)
            return;

        // IF LAYER WAS JUST BEATEN
        if (LayerFinished)
        {
            LAYER_BEATEN = true;

            // IF BOSS LAYER
            if (mobManager.waveData.LayerType == LayerType.Boss)
            {
                audio.SwitchToGameMusic();

                ui.GiftCardDisplayed = true;
                ui.GiftTitle = RewardService.GiveRewards(mobManager.waveData.Gifts, player, weaponsManager);
            }
        }
    }

    public List<IDrawableLayer> GetDrawables()
    {
        var list = new List<IDrawableLayer>();

        list.Add(this);

        foreach (var p in platforms)
            list.Add(p);
        
        return list;
    }

    // ONLY the background. Every platform is handed out by GetDrawables above
    // as a drawable in its own right, so drawing them here as well submitted
    // each one twice - once behind the mobs, once in front of them.
    public void Draw()
    {
        _background.Draw(Globals.BackgroundOverlayColor);
    }
}