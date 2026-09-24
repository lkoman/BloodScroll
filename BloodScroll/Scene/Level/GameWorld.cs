using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

//
// Generates and switches between layers, updates and draws current layer
// - HOLDS DATA OF ALL LAYERS, tracks current layer
// - HOLDS ALL MOB WAVE DATA (from json file, to send to specific layer when generating it)
// - TRACKS ALL MOB PROJECTILES
//

public class GameWorld
{
    // BESIDE THE GAME, NOT BESIDE WHOEVER LAUNCHED IT. A relative path is
    // resolved against the working directory, and a shortcut or a launcher sets
    // one that is not the game folder - the read then throws and takes the run
    // down on the frame PLAY is pressed. Same reasoning as SaveManager.SavePath,
    // and the same thing TitleContainer does for every atlas.
    private static readonly string WaveFile =
        Path.Combine(AppContext.BaseDirectory, "Content", "mobWaves.json");

    // Layers this far above and below the player are on screen
    private const int VisibleLayerRange = 1;

    private static readonly JsonSerializerOptions WaveJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private IAudioService audioService;

    // Mobs get the world every frame but not the audio service. A mob that hurts
    // the player from inside its own update (the moth's gust) needs it, because
    // Player.TakeDamage is what plays the sound.
    public IAudioService Audio => audioService;
    private List<WaveData> wavesData;
    private int maxAuthoredLayerIndex;
    public int maxLayerGenerated = 1;
    private List<Layer> layers = [];
    public IReadOnlyList<Layer> Layers => layers;
    private readonly int numOfPaths = 2; // Two paths, each on one half of the screen
    public Platform bigPlatform = new();

    // The position of the last small platform generated, for generating a new layer
    private Vector2[] lastPlatformPos;

    // ALL PROJECTILES FROM ENEMIES
    public List<Bullet> MobProjectiles { get; set; } = [];

    // EVERYTHING THAT GOES OFF. Lives here rather than with the weapons because
    // a blast has to reach every mob on every nearby layer, and only the world
    // can see them all.
    private readonly List<FlowerBomb> bombs = [];
    private readonly List<Explosion> explosions = [];

    // Butterfly dust. Purely a picture - it lives here because the butterfly is
    // gone the instant it bursts.
    private readonly List<GoldDust> dust = [];

    public void LoadContent(IAudioService audio)
    {
        audioService = audio;

        StartNewWorld();

        Platform tmpBigPlatform = new();
        tmpBigPlatform = tmpBigPlatform.GenerateNewPlatform("platform", new Vector2(0, 0));
        bigPlatform = bigPlatform.GenerateNewPlatform("platform", new Vector2(0, Core.windowHeight - tmpBigPlatform.Height));
    }

    public void Restart()
    {
        StartNewWorld();
    }

    // Everything a fresh run needs: reload the wave table, build two layers
    private void StartNewWorld()
    {
        maxLayerGenerated = 1;
        layers.Clear();
        MobProjectiles.Clear();
        bombs.Clear();
        explosions.Clear();
        dust.Clear();

        lastPlatformPos = new Vector2[numOfPaths];

        // Gifts are earned again every run, so bosses start at their written HP
        BossTally.Reset();

        // Every layer above the ground is generated now, so the watermark that
        // skips spent layers starts back at the bottom
        lowestLiveLayer = 0;

        LoadWaveData();

        // Generate first two layers
        GenerateNewLayer(Globals.CurrentLayerIndex);
        layers[Globals.CurrentLayerIndex].MobManager.wavesTriggered = true;

        GenerateNewLayer(Globals.CurrentLayerIndex + 1);
    }

    private void LoadWaveData()
    {
        var jsonText = File.ReadAllText(WaveFile);
        wavesData = JsonSerializer.Deserialize<List<WaveData>>(jsonText, WaveJsonOptions) ?? [];

        maxAuthoredLayerIndex = wavesData.Count > 0 ? wavesData.Max(e => e.LayerIndex) : -1;
    }

    public void UpdateLevel(IPlayer player, IWeaponsManager weaponsManager, IUI ui)
    {
        GenerateLayers();

        // BEFORE the layers, so a mob killed by a blast is cleared on the same
        // frame the blast landed
        UpdateBombs(player);
        UpdateExplosions(player);
        UpdateDust();

        UpdateLayers(player, weaponsManager, ui);
        UpdateMobProjectiles();
    }

    public List<IDrawableLayer> GetDrawables()
    {
        // Add all monster bullets
        var list = new List<IDrawableLayer>(MobProjectiles);

        // Bombs waiting to go off, the fireballs they left, and the butterfly dust
        list.AddRange(bombs);
        list.AddRange(explosions);
        list.AddRange(dust);

        // Add Layer background + all platforms
        list.AddRange(layers[Globals.CurrentLayerIndex].GetDrawables());

        // Add next and prev layers
        list.AddRange(layers[Globals.CurrentLayerIndex + 1].GetDrawables());
        if (Globals.CurrentLayerIndex > 0)
        {
            list.AddRange(layers[Globals.CurrentLayerIndex - 1].GetDrawables());
        }

        // Add the monsters that are actually near the player
        foreach (var layer in ActiveLayers())
            list.AddRange(layer.MobManager.GetDrawables());

        return list;
    }

    //
    // THE LOWEST LAYER THAT COULD STILL HAVE ANYTHING ON IT
    //
    // Everything below this is spent: its waves are all sent and everything
    // they sent is dead. A layer can never come back to life - the only thing
    // that puts a new mob on a layer is a mob already standing on it (see
    // MobManager.RequestSpawn), and there are none left - so once one goes
    // quiet it can be stepped over for the rest of the run.
    //
    // This is what keeps the per frame cost flat. Without it every frame walked
    // the whole tower from the ground up, and the walk got longer the higher
    // the player climbed.
    //
    private int lowestLiveLayer = 0;

    // Every layer still running, plus the ones on screen.
    // Unfinished layers are ALWAYS included however far below the player they
    // are, so mobs he ran from keep coming. Cleared layers drop out.
    public IEnumerable<Layer> ActiveLayers()
    {
        int firstVisible = Globals.CurrentLayerIndex - VisibleLayerRange;
        int lastVisible = Globals.CurrentLayerIndex + VisibleLayerRange;

        // Walk the watermark up past anything that has gone quiet for good.
        // Never past a layer that is on screen - those are yielded whether or
        // not they still have anything left to update, because they are drawn.
        while (lowestLiveLayer < layers.Count &&
               lowestLiveLayer < firstVisible &&
               !layers[lowestLiveLayer].NeedsUpdate)
        {
            lowestLiveLayer++;
        }

        // NEVER STARTS ABOVE WHAT IS ON SCREEN. The player can fall back down
        // past the watermark, and a visible layer has to be yielded whatever
        // the watermark has reached - otherwise this would be leaning on
        // "spent layers are empty anyway" to stay correct, which is true today
        // and is not the sort of thing to build on.
        int from = Math.Min(lowestLiveLayer, Math.Max(0, firstVisible));

        // The top end needs no watermark of its own: only one layer is ever
        // generated above the player, so the list stops just over his head.
        for (int i = from; i < layers.Count; i++)
        {
            bool onScreen = i >= firstVisible && i <= lastVisible;

            if (onScreen || layers[i].NeedsUpdate)
                yield return layers[i];
        }
    }

    private void UpdateLayers(IPlayer player, IWeaponsManager weaponsManager, IUI ui)
    {
        foreach (var layer in ActiveLayers())
        {
            layer.Update(player, weaponsManager, ui, this, audioService);
        }
    }

    // MOB PROJECTILES
    //
    // Every shooter names its own tint - a bullet is a plain circle, so colour
    // is the only thing saying where it came from (see Globals). Null keeps
    // whatever the effect gives it (poison shots are always green).
    public void SpawnMonsterBullet(Vector2 spawn, Vector2 playerPos, string projectileType, int PROJECTILE_DAMAGE, float PROJECTILE_SPEED, AudioId soundType, BulletEffect effect = BulletEffect.Damage, Color? tint = null)
    {
        //audioService.PlaySound(soundType);

        MobProjectiles.Add(new Bullet());
        MobProjectiles.Last().LoadContent(
            spawn,
            playerPos,
            projectileType,
            PROJECTILE_DAMAGE,
            PROJECTILE_SPEED,
            effect
        );

        // After LoadContent, so a named colour outranks the effect's
        if (tint.HasValue)
            MobProjectiles.Last().Tint = tint.Value;
    }

    // Lets one mob find another on its own layer after they were spawned
    // separately (the moth looking for its cocoon).
    //
    // The optional test is for when the FIRST one of a kind is not the right
    // one: two moths on a late layer each need a cocoon of their own, so each
    // asks for one nobody has claimed yet rather than for any cocoon at all.
    public T FindMobOnLayer<T>(int layerIndex, Func<T, bool> where = null) where T : class, IMob
    {
        if (layerIndex < 0 || layerIndex >= layers.Count)
            return null;

        foreach (var mob in layers[layerIndex].MobManager.mobs)
        {
            if (mob is T match && (where == null || where(match)))
                return match;
        }

        return null;
    }

    // Lets a mob spawn another without touching the list being iterated. The new
    // mob is placed with its MIDDLE on the point.
    public void RequestMobSpawn(int layerIndex, MobType type, Vector2 centre)
    {
        if (layerIndex < 0 || layerIndex >= layers.Count)
            return;

        layers[layerIndex].MobManager.RequestSpawn(type, centre, layerIndex);
    }

    //
    // BOMBS AND BLASTS
    //

    // A shell going off wherever it got to. Asked for by whoever noticed - the
    // weapons manager on a spent fuse, the collision response on a hit.
    // Its DAMAGE is the blast's; the shell itself never hits.
    public void Detonate(Bullet shell)
    {
        SpawnExplosion(shell.Centre, shell.DAMAGE, shell.BlastRadius, Globals.BlastOrange);
    }

    public void SpawnExplosion(Vector2 centre, int damage, float radius, Color colour, bool hurtsPlayer = false, int playerDamage = 0)
    {
        if (radius <= 0f)
            return;

        explosions.Add(new Explosion(centre, damage, radius, colour, hurtsPlayer, playerDamage));
        audioService.PlaySound(AudioId.FireHit);
    }

    private void UpdateBombs(IPlayer player)
    {
        // PUTTING ONE DOWN BEATS PICKING ONE UP - the world asks for the key
        // press before any flower sees it
        if (player.HasBomb && player.TryTakeInteract())
        {
            bombs.Add(new FlowerBomb(player.Position + new Vector2(player.Width, player.Height) * 0.5f));
            player.UseBomb();
        }

        for (int i = bombs.Count - 1; i >= 0; i--)
        {
            bombs[i].Update();

            if (!bombs[i].FuseSpent)
                continue;

            SpawnExplosion(
                bombs[i].Centre,
                FlowerBomb.DAMAGE,
                FlowerBomb.RADIUS,
                Globals.HotPink,
                hurtsPlayer: true,
                playerDamage: FlowerBomb.PLAYER_DAMAGE);

            bombs.RemoveAt(i);
        }
    }

    // Nothing lands and nothing is healed here - the butterfly does that itself.
    // The dust is only updated and drawn until it blows away.
    public void SpawnGoldDust(Vector2 centre)
    {
        dust.Add(new GoldDust(centre));
    }

    private void UpdateDust()
    {
        for (int i = dust.Count - 1; i >= 0; i--)
        {
            dust[i].Update();

            if (dust[i].Finished)
                dust.RemoveAt(i);
        }
    }

    private void UpdateExplosions(IPlayer player)
    {
        for (int i = explosions.Count - 1; i >= 0; i--)
        {
            if (explosions[i].NeedsToLand)
                LandBlast(explosions[i], player);

            explosions[i].Update();

            if (explosions[i].Finished)
                explosions.RemoveAt(i);
        }
    }

    // ONE CIRCLE, ONE HIT, on the frame the blast appeared. After that the
    // fireball is only a picture - see Explosion.
    private void LandBlast(Explosion blast, IPlayer player)
    {
        blast.NeedsToLand = false;

        foreach (var layer in ActiveLayers())
        {
            foreach (var mob in layer.MobManager.mobs)
            {
                if (mob.CollidesWith(blast.Bounds))
                    mob.TakeDamage(blast.Damage, audioService);
            }
        }

        if (blast.HurtsPlayer && player.HurtBox.Intersects(blast.Bounds))
            player.TakeDamage(blast.PlayerDamage, audioService);
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
        // Hand authored settings win. Past the end of the JSON the boss schedule
        // takes over so the climb keeps producing bosses.
        WaveData settings = wavesData.FirstOrDefault(e => e.LayerIndex == layerIndex)
                            ?? BossSchedule.Generate(layerIndex, maxAuthoredLayerIndex);

        layers.Add(new Layer());
        lastPlatformPos = layers.Last().GenerateLayer(
            layerIndex,
            lastPlatformPos,
            settings
        );
    }

    private bool FirstTimeOnLayer()
    {
        // This means that no layers are generated above the current layer
        return Globals.CurrentLayerIndex == maxLayerGenerated;
    }

    public string GetCurrentLayerType()
    {
        return layers[Globals.CurrentLayerIndex].MobManager.waveData.LayerType.ToDisplayString();
    }

    public IReadOnlyList<Platform> GetCurrentLayerPlatformList()
    {
        return layers[Globals.CurrentLayerIndex].Platforms;
    }

    private bool IsLayerBossLayer(int i)
    {
        return layers[i].MobManager.waveData.LayerType == LayerType.Boss;
    }
}
