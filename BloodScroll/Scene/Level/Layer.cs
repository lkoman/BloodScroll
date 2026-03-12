using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

using Vector2 = Microsoft.Xna.Framework.Vector2;

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
    public string layerType;
    public bool LAYER_BEATEN = false;

    // BACKGROUND
    private Sprite _background;

    // PLATFORMS on this layer
    private List<Platform> platforms = [];
    public IReadOnlyList<Platform> Platforms => platforms;

    // MOBS on this layer
    private MobManager mobManager;
    public MobManager MobManager => mobManager;


    // lastPlatformPosition is the position of the last generated platform of the previous layer,
        // to start the generation of this layer (for continuous platform generation)
    public Vector2[] GenerateLayer(int currentLayerIndex, Vector2[] lastPlatformPos, WaveData waveSettings)
    {
        layerIndex = currentLayerIndex;
        layerOffset = new Vector2(0f, currentLayerIndex * Core.windowHeight);

        // GENERATE LAYER
        _background = LayerGenerator.GetBackground(currentLayerIndex, layerOffset);
        (platforms, lastPlatformPos) = LayerGenerator.GeneratePlatforms(currentLayerIndex, layerOffset, lastPlatformPos);

        // SET MOB MANAGER FOR THIS LAYER
        mobManager = new(layerIndex, waveSettings);

        // All sleeping bats need to be generated first, then we wake them up
        mobManager.GenerateAllSleepingBats(layerIndex);

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
        if (mobManager.waveData.WavesToBeat <= 0 && mobManager.AllEnemiesBeaten)
        {
            LAYER_BEATEN = true;

            // IF BOSS LAYER
            if (mobManager.waveData.LayerType == "Boss Layer")
            {
                audio.SwitchToGameMusic();

                ui.GiftCardDisplayed = true;
                GiveLayerRewards(player, weaponsManager, ui);
            }
        }
    }

    private void GiveLayerRewards(IPlayer player, IWeaponsManager weaponsManager, IUI ui)
    {
        ui.GiftTitle = "- Heal\n";
        // GET GIFTS / TREASURES FROM BEATING A LEVEL
        if (mobManager.waveData.GunID.HasValue)
        {
            ui.GiftTitle += "- New Gun\n";
            weaponsManager.UnlockNewWeapon(mobManager.waveData.GunID.Value);
        }

        if (mobManager.waveData.IncreasedHP.HasValue)
        {
            ui.GiftTitle += "- Increased HP\n";
            player.IncreaseMaxHP(mobManager.waveData.IncreasedHP.Value);
        }
        
        player.Heal();
    }

    public List<IDrawableLayer> GetDrawables()
    {
        var list = new List<IDrawableLayer>();

        list.Add(this);

        foreach (var p in platforms)
            list.Add(p);
        
        return list;
    }

    public void Draw()
    {
        _background.Draw(Globals.BackgroundOverlayColor);

        foreach (var p in platforms)
        {
            p.Draw();
        }
    }
}