using System;
using MonoGameLibrary;

public class WaveData
{
    // LAYER DATA
    public int LayerIndex { get; set; }
    public string LayerType { get; set; }

    // WAVE DATA
    public int? WavesToBeat { get; set; }

    // GIFTS THAT THE PLAYER GETS WHEN BEATING A LEVEL
    public int? GunID { get; set; } // Player gets a new gun
    public int? IncreasedHP { get; set; } // Player gets the gift of higher max HP

    // NUMBER OF ENEMIES ON THIS LEVEL PER WAVE
    public int? Centipides_num { get; set; }
    public int? Slimes_num { get; set; }
    public int? Bats_num { get; set; }
    public int? PurpleBats_num { get; set; }
    public int? Fireball_num { get; set; }
    public int? Fireboss_num { get; set; }

    // WAVE TIMER
    public float? WavesTimerSeconds { get; set; }
    public float? WavesTimer { get; set; }

    public WaveData(int layerIndex)
    {   
        LayerIndex = layerIndex;
        LayerType = "Layer";

        // Waves timer seconds je enako kot st waves to beat (se veča slightly, da imaš malo več časa)
        WavesTimerSeconds = (LayerIndex / 5) + 2;
        WavesTimer = 0.0f;

        SetDifficutly();
    }

    public void SetDifficutly()
    {
        // Začnemo z base = 2, base+1 vsak wave do boss-a, po vsakem bossu je base++
            // 0 = 2 bats per wave + difficulty (0, 1, 2)
            // 1 = 3 bats per wave + difficulty (0, 1, 2)
            // 2 = 4 bats per wave + difficulty (0, 1, 2)
            // 3 = 5 bats per wave + difficulty (0, 1, 2)
            // 4 = boss level, tam se vse nastavi ročno
            // 5 = 3 bats per wave + difficulty (0, 1, 2)
            // 6 = 4 bats per wave + difficulty (0, 1, 2)
            // 7 = 5 bats per wave + difficulty (0, 1, 2)
            // 8 = 6 bats per wave + difficulty (0, 1, 2)
            // 9 = boss level
        Bats_num = (LayerIndex / 5) + 2 + Globals.DIFFICULTY + (LayerIndex % 5);

        // Začnemo z base = 0, base+1 vsak wave do boss-a, po vsakem bossu je base++
            // 0 = 0 bats per wave + difficulty (0, 1, 2)
            // 1 = 1 bats per wave + difficulty (0, 1, 2)
            // 2 = 2 bats per wave + difficulty (0, 1, 2)
            // 3 = 3 bats per wave + difficulty (0, 1, 2)
            // 4 = boss level, tam se vse nastavi ročno
            // 5 = 1 bats per wave + difficulty (0, 1, 2)
            // 6 = 2 bats per wave + difficulty (0, 1, 2)
            // 7 = 3 bats per wave + difficulty (0, 1, 2)
            // 8 = 4 bats per wave + difficulty (0, 1, 2)
            // 9 = boss level
        PurpleBats_num = (LayerIndex / 5) + 0 + Globals.DIFFICULTY + (LayerIndex % 5);

        // difficulty (0, 1, 2) slime in difficulty (0, 1, 2) centipide na wave vedno
        Slimes_num = Globals.DIFFICULTY;
        Centipides_num = Globals.DIFFICULTY;

        // Začnemo z dvema wave-oma, po vsakem bossu + 1 wave
            // 0...4 = 2 waves + difficulty (0, 1, 2)
            // 5...9 = 3 waves + difficulty (0, 1, 2)
            // 10...14 = 4 waves + difficulty (0, 1, 2)
            // 4, 9, 14, ... = BOSS LAYERS, tam se vse nastavi ročno
        WavesToBeat = (LayerIndex / 5) + 2 + Globals.DIFFICULTY;
    }
}
