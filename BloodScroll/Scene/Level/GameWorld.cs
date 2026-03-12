
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using MonoGameLibrary;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;

namespace BloodScroll;

//
// Generates and switches between layers, updates and draws current layer
// - HOLDS DATA OF ALL LAYERS, tracks current layer
// - HOLDS ALL MOB WAVE DATA (from json file, to send to specific layer when generating it)
// - TRACKS ALL MOB PROJECTILES
//

public class GameWorld
{
    private IAudioService audioService;
    private List<WaveData> wavesData;
    public int maxLayerGenerated = 1;
    private List<Layer> layers = [];
    public IReadOnlyList<Layer> Layers => layers;
    private readonly int numOfPaths = 2; // Two paths, each on one half of the screen
    public Platform bigPlatform = new();

    // The positioan of the last small platform generated, for generating a new layer
    private Vector2[] lastPlatformPos;

    // ALL PROJECTILES FROM ENEMIES FROM THIS LAYER
    public List<Bullet> MobProjectiles { get; set; } = [];

    public void LoadContent(IAudioService audio)
    {
        lastPlatformPos = new Vector2[numOfPaths];
        audioService = audio;

        var jsonText = File.ReadAllText("Content/mobWaves.json");
        wavesData = JsonSerializer.Deserialize<List<WaveData>>(jsonText);

        // Generate first two layers
        GenerateNewLayer(Globals.CurrentLayerIndex);
        layers[Globals.CurrentLayerIndex].MobManager.wavesTriggered = true;

        GenerateNewLayer(Globals.CurrentLayerIndex + 1);

        Platform tmpBigPlatform = new();
        tmpBigPlatform = tmpBigPlatform.GenerateNewPlatform("platform", new Vector2(0, 0));
        bigPlatform = bigPlatform.GenerateNewPlatform("platform", new Vector2(0, Core.windowHeight - tmpBigPlatform.Height));
    }

    public void Restart()
    {
        maxLayerGenerated = 1;
        layers.Clear();
        MobProjectiles.Clear();

        var jsonText = File.ReadAllText("Content/mobWaves.json");
        wavesData = JsonSerializer.Deserialize<List<WaveData>>(jsonText);

        // Generate first two layers
        GenerateNewLayer(Globals.CurrentLayerIndex);
        layers[Globals.CurrentLayerIndex].MobManager.wavesTriggered = true;

        GenerateNewLayer(Globals.CurrentLayerIndex + 1);
    }

    public void UpdateLevel(IPlayer player, IWeaponsManager weaponsManager, IUI ui)
    {
        GenerateLayers();
        UpdateLayers(player, weaponsManager, ui);
        UpdateMobProjectiles();
    }

    public List<IDrawableLayer> GetDrawables()
    {
        var list = new List<IDrawableLayer>();

        // Add all monster bullets
        foreach (Bullet bullet in MobProjectiles) {
            list.Add(bullet);
        }

        // Add Layer background + all platforms
        list.AddRange(layers[Globals.CurrentLayerIndex].GetDrawables());

        // Add next and prev layers
        list.AddRange(layers[Globals.CurrentLayerIndex + 1].GetDrawables());
        if (Globals.CurrentLayerIndex > 0)
        {
            list.AddRange(layers[Globals.CurrentLayerIndex - 1].GetDrawables());
        }
        
        // Add all monsters
        foreach (var layer in layers)
            list.AddRange(layer.MobManager.GetDrawables());

        return list;
    }

    private void UpdateLayers(IPlayer player, IWeaponsManager weaponsManager, IUI ui)
    {
        // UPDATE ALL LAYERS
        foreach (var layer in layers)
        {
            layer.Update(player, weaponsManager, ui, this, audioService);
        }
    }

    // MOB PROJECTILES
    public void SpawnMonsterBullet(Vector2 spawn, Vector2 playerPos, string projectileType, int PROJECTILE_DAMAGE, float PROJECTILE_SPEED, AudioId soundType)
    {
        //audioService.PlaySound(soundType);

        MobProjectiles.Add(new Bullet());
        MobProjectiles.Last().LoadContent(
            spawn,
            playerPos,
            projectileType,
            PROJECTILE_DAMAGE, 
            PROJECTILE_SPEED
        );
    }

    private void UpdateMobProjectiles()
    {
        // Update all bullets (or remove them)
        for (int i = MobProjectiles.Count - 1; i >= 0; i--) {
            if (MobProjectiles[i].active == 1)
                MobProjectiles[i].Update();
            else
                MobProjectiles.RemoveAt(i);
        }
    }

    private void GenerateLayers()
    {
        // FIRST TIME ON LAYER
        // If we are on the layer for the first time, generate new layer above it
        if (FirstTimeOnLayer())
        {
            if (IsLayerBossLayer(Globals.CurrentLayerIndex)) {
                audioService.SwitchToBossMusic();
            }

            GenerateNewLayer(Globals.CurrentLayerIndex + 1);
            maxLayerGenerated ++;

            // TRIGGER MOB WAVES ON THIS WAVE
            layers[Globals.CurrentLayerIndex].MobManager.wavesTriggered = true;
        }
    }

    private void GenerateNewLayer(int layerIndex)
    {
        layers.Add(new Layer());
        lastPlatformPos = layers.Last().GenerateLayer(
            layerIndex,
            lastPlatformPos, 
            wavesData.FirstOrDefault(e => e.LayerIndex == layerIndex) // send the correct wave data
        );
    }

    private bool FirstTimeOnLayer()
    {
        // This means that no layers are generated above the current layer
        return Globals.CurrentLayerIndex == maxLayerGenerated;
    }

    public string GetCurrentLayerType()
    {
        return layers[Globals.CurrentLayerIndex].MobManager.waveData.LayerType;
    }

    public IReadOnlyList<Platform> GetCurrentLayerPlatformList()
    {
        return layers[Globals.CurrentLayerIndex].Platforms;
    }

    private bool IsLayerBossLayer(int i)
    {
        return layers[i].MobManager.waveData.LayerType == "Boss Layer";
    }
}