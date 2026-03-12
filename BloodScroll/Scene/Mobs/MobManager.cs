using Microsoft.Xna.Framework;
using System.Collections.Generic;
using MonoGameLibrary;
using System.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Security.Cryptography;

namespace BloodScroll;

//
// Handles spawning, updating, drawing of mobs ON A LAYER
//

public class MobManager
{
    // LIST OF REFERENCES TO ALL MOBS
    public readonly List<IMob> mobs = [];

    public WaveData waveData;
    
    private readonly List<Bat> bats = [];
    private int bats_index = 0;
    private readonly List<PurpleBat> purpleBats = [];
    private int purpleBats_index = 0;
    private Vector2 playerPos;
    // MOB WAVES
    public bool wavesTriggered = false;
    public bool AllEnemiesBeaten = false;

    public MobManager(int currentLayerIndex, WaveData data)
    {
        waveData = new(currentLayerIndex);
        SetWaveSettings(data);
    }

    public void Update(IPlayer player, int layerIndex, GameWorld gameWorld)
    {   
        playerPos = player.Position;

        WaveTimer(layerIndex, player.Position);

        if (mobs.Count == 0)
        {
            AllEnemiesBeaten = true;
            return;
        }
        AllEnemiesBeaten = false;

        // Update all mobs and remove if killed
        for (int i = mobs.Count - 1; i >= 0; i--)
        {
            IMob mob = mobs[i];
            mob.Update(playerPos, gameWorld);

            if (mob.HP <= 0)
            {
                mobs.RemoveAt(i);
                Globals.POINTS += mob.PointsOnKill;
            }
        }
    }

    public List<IDrawableLayer> GetDrawables()
    {
        var list = new List<IDrawableLayer>();

        // Add all monsters
        foreach (var m in mobs)
            list.Add(m);

        return list;
    }

    private void WaveTimer(int layerIndex, Vector2 playerPos)
    {
        // TIMER THAT CHANGES TARGET DIRECTION
        waveData.WavesTimer += Globals.DT;

        if (waveData.WavesTimer >= waveData.WavesTimerSeconds)
        {
            if (waveData.WavesToBeat >= 1)
            {
                GenerateWave(layerIndex, playerPos);
                waveData.WavesToBeat --;
            }
            waveData.WavesTimer = 0.0f;
        }
    }

    public void GenerateAllSleepingBats(int layerIndex)
    {
        bats.Clear();
        bats_index = 0;
        purpleBats.Clear();
        purpleBats_index = 0;
        GenerateBats(waveData.WavesToBeat * waveData.Bats_num ?? 0, layerIndex);
        GeneratePurpleBats(waveData.WavesToBeat * waveData.PurpleBats_num ?? 0, layerIndex);
    }

    public void GenerateWave(int layerIndex, Vector2 playerPos)
    {
        GenerateCentipides(waveData.Centipides_num ?? 0, layerIndex);
        GenerateSlimes(waveData.Slimes_num ?? 0, layerIndex);
        GenerateFireballs(waveData.Fireball_num ?? 0, layerIndex);
        GenerateFirebosses(waveData.Fireboss_num ?? 0, layerIndex);

        // WAKE UP BATS
        for (int i = 0; i < (waveData.Bats_num ?? 0); i++)
        {
            bats[bats_index].WakeUpBat(playerPos);
            bats_index ++;
        }
        for (int i = 0; i < (waveData.PurpleBats_num ?? 0); i++)
        {
            purpleBats[purpleBats_index].WakeUpBat(playerPos);
            purpleBats_index ++;
        }
    }

    public void GenerateCentipides(int centipidesNum, int spawnLayer)
    {
        // CENTIPIDES
        for (int i = 0; i < centipidesNum; i++)
        {
            mobs.Add(new Centipide());
            mobs.Last().LoadContent(playerPos, spawnLayer);
        }
    }
    public void GenerateSlimes(int slimesNum, int spawnLayer)
    {
        // SLIMES
        for (int i = 0; i < slimesNum; i++)
        {
            mobs.Add(new Slime());
            mobs.Last().LoadContent(playerPos, spawnLayer);
        }
    }
    public void GenerateBats(int batsNum, int spawnLayer)
    {
        // BATS
        for (int i = 0; i < batsNum; i++)
        {
            bats.Add(new Bat());
            bats.Last().LoadContent(playerPos, spawnLayer);

            mobs.Add(bats.Last());
        }
    }

    public void GeneratePurpleBats(int batsNum, int spawnLayer)
    {
        // PURPLE BATS
        for (int i = 0; i < batsNum; i++)
        {
            purpleBats.Add(new PurpleBat());
            purpleBats.Last().LoadContent(playerPos, spawnLayer);

            mobs.Add(purpleBats.Last());
        }
    }
    public void GenerateFireballs(int fireballsNum, int spawnLayer) {
        // TENTACLES
        for (int i = 0; i < fireballsNum; i++)
        {
            mobs.Add(new Fireball());
            mobs.Last().LoadContent(playerPos, spawnLayer);
        }
    }
    public void GenerateFirebosses(int firebossNum, int spawnLayer) {
        // TENTACLES
        for (int i = 0; i < firebossNum; i++)
        {
            mobs.Add(new Fireboss());
            mobs.Last().LoadContent(playerPos, spawnLayer);
        }
    }
    private void SetWaveSettings(WaveData data)
    {
        if (data != null)
        {
            waveData.LayerIndex = data.LayerIndex;
            waveData.LayerType = data.LayerType ?? waveData.LayerType;
            waveData.GunID = data.GunID ?? waveData.GunID;
            waveData.IncreasedHP = data.IncreasedHP ?? waveData.IncreasedHP;
            waveData.Centipides_num = data.Centipides_num ?? waveData.Centipides_num;
            waveData.Slimes_num = data.Slimes_num ?? waveData.Slimes_num;
            waveData.Bats_num = data.Bats_num ?? waveData.Bats_num;
            waveData.PurpleBats_num = data.PurpleBats_num ?? waveData.PurpleBats_num;
            waveData.Fireball_num = data.Fireball_num ?? waveData.Fireball_num;
            waveData.Fireboss_num = data.Fireboss_num ?? waveData.Fireboss_num;
            waveData.WavesToBeat = data.WavesToBeat ?? waveData.WavesToBeat;
            waveData.WavesTimerSeconds = data.WavesTimerSeconds ?? waveData.WavesTimerSeconds;
        }

    }

    public void SetDifficulty()
    {
        waveData.SetDifficutly();
    }
}