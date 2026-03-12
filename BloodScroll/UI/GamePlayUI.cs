using System;
using System.Dynamic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

public class GamePlayUI()
{
    private string LevelString;

    public void Draw(IPlayer player)
    {
        // WRITE LEVEL NUMBER AND TYPE
        LevelString = Globals.CurrentLayerType + " (layer " + Globals.CurrentLayerIndex.ToString() + ")";
        if (Globals.CurrentLayerType == "Boss Layer")
        {
            LevelString = Globals.CurrentLayerType;
        }

        Globals.SpriteBatch.DrawString(
            UISettings.fontUI,
            LevelString,
            new Vector2(
                Globals.VIRTUAL_WIDTH / 2 - UISettings.fontUI.MeasureString(LevelString).X / 2,
                50),
            Color.White
        );

        // POINTS
        Globals.SpriteBatch.DrawString(
            UISettings.fontUI,
            Globals.POINTS.ToString(),
            new Vector2(Globals.VIRTUAL_WIDTH - UISettings.fontUI.MeasureString(Globals.POINTS.ToString()).X - 50, 50),
            Color.White
        );

        // HP
        Globals.SpriteBatch.DrawString(
            UISettings.fontUI,
            "HP: " + player.HP.ToString() + "/" + player.MaxHP.ToString(),
            new Vector2(50, 50),
            Color.White
        );
    }

    public static void DrawBossHP(int HP, float x, float y)
    {
        Globals.SpriteBatch.DrawString(
            UISettings.fontUI,
            HP.ToString(),
            new Vector2(x - UISettings.fontUI.MeasureString(HP.ToString()).X / 2, y),
            Color.White
        );
    }
}