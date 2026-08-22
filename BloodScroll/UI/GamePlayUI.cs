using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE IN GAME HUD
// Layer name in the middle, points on the right, HP and the gifts you are
// carrying on the left, and what E does right now along the bottom.
//

public class GamePlayUI
{
    private string LevelString;

    // 1x1 white pixel stretched into bars, same trick the buttons use
    private Texture2D _pixel;

    private const int HUD_LEFT = 50;
    private const int HUD_TOP = 50;
    private const int BAR_WIDTH = 320;
    private const int BAR_HEIGHT = 14;

    // The gun name beside its cooldown bar. Small - the colour is what is
    // actually being read, the word is only there for the first time you see it.
    private const float LABEL_SCALE = 0.5f;

    //
    // WHAT E DOES RIGHT NOW
    //
    // Along the bottom middle of the screen rather than over the flower or over
    // the player's head: this is HUD, drawn in screen space, so it sits in the
    // same place every time and does not go chasing a swinging flower head or
    // wander off the top of the screen when the player is standing high up.
    //
    private const string PLUCK_TEXT = "Press E to pick up";
    private const string PLANT_TEXT = "Press E to plant the bomb";

    // How far the bottom of the text sits above the bottom edge
    private const int PROMPT_BOTTOM = 90;

    // Latched by the flower the player is standing on while the world updates,
    // and cleared the moment it has been drawn - so the offer lives exactly one
    // frame and never shows for a frame in which no flower made it.
    private static bool pluckOffered = false;

    // Called by Flower while the player is in reach of pulling it up
    public static void OfferPluck() => pluckOffered = true;

    public void LoadContent(GraphicsDevice device)
    {
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public void Draw(IPlayer player, IWeaponsManager weapons)
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
                HUD_TOP),
            Color.White
        );

        // POINTS
        Globals.SpriteBatch.DrawString(
            UISettings.fontUI,
            Globals.POINTS.ToString(),
            new Vector2(Globals.VIRTUAL_WIDTH - UISettings.fontUI.MeasureString(Globals.POINTS.ToString()).X - 50, HUD_TOP),
            Color.White
        );

        // HP
        string hpText = "HP: " + player.HP.ToString() + "/" + player.MaxHP.ToString();
        Globals.SpriteBatch.DrawString(UISettings.fontUI, hpText, new Vector2(HUD_LEFT, HUD_TOP), Color.White);

        float y = HUD_TOP + UISettings.fontUI.MeasureString(hpText).Y + 6;

        y = DrawHealthBar(player, y);
        y = DrawShieldBar(player, y);
        y = DrawWeaponCooldowns(weapons, y);

        DrawStatusLine(player, y);
        DrawInteractPrompt(player);
    }

    //
    // THE E PROMPT
    //
    // Carrying a bomb wins over standing on a flower, because the world takes
    // the key press before any flower gets a look at it - a player with full
    // hands always puts down what he is holding, so that is what it must say.
    //
    private static void DrawInteractPrompt(IPlayer player)
    {
        string text = null;

        if (player.HasBomb)
            text = PLANT_TEXT;

        else if (pluckOffered)
            text = PLUCK_TEXT;

        // Cleared whether it was used or not: the flower has to make the offer
        // again next frame for the prompt to stay up
        pluckOffered = false;

        if (text == null)
            return;

        Vector2 size = UISettings.fontUI.MeasureString(text);
        Vector2 at = new(
            Globals.VIRTUAL_WIDTH / 2f - size.X / 2f,
            Globals.VIRTUAL_HEIGHT - PROMPT_BOTTOM - size.Y);

        // Dropped shadow first, so it stays readable over a bright background
        Globals.SpriteBatch.DrawString(UISettings.fontUI, text, at + new Vector2(2f, 2f), Globals.AlmostBlack);
        Globals.SpriteBatch.DrawString(UISettings.fontUI, text, at, Globals.AlmostWhite);
    }

    private float DrawHealthBar(IPlayer player, float y)
    {
        DrawBar(y, player.HP / (float)player.MaxHP, Globals.DarkGray, Globals.Red);
        return y + BAR_HEIGHT + 4;
    }

    private float DrawShieldBar(IPlayer player, float y)
    {
        if (player.ShieldMax <= 0)
            return y;

        // Same yellow as the bubble around the player, so the bar and the ring
        // are obviously the same thing
        DrawBar(y, player.Shield / (float)player.ShieldMax, Globals.DarkGray, Globals.Yellow);
        return y + BAR_HEIGHT + 4;
    }

    //
    // ONE BAR PER SLOW GUN, under the health and the shield
    //
    // Stacked in the order they are unlocked, so the shell sits under the shield
    // and the stun sits under the shell and neither ever moves once it appears.
    // Each bar is drawn in ITS OWN GUN'S COLOUR, the same colour as the gun in
    // the player's hands and the shots coming out of it - there is no label to
    // read in the middle of a fight, so the colour has to be the label.
    //
    // A full bar means the gun is ready. It fills as the gun recharges rather
    // than draining, because the thing worth glancing at is whether it is up.
    //
    private float DrawWeaponCooldowns(IWeaponsManager weapons, float y)
    {
        if (weapons == null)
            return y;

        foreach (WeaponCooldown gun in weapons.Cooldowns)
        {
            // Dimmed while it is still filling, full brightness the instant it
            // is ready - the bar finishing is a thing you can catch out of the
            // corner of your eye
            bool ready = gun.Ready >= 1f;
            Color colour = ready ? gun.Colour : gun.Colour * 0.65f;

            DrawBar(y, gun.Ready, Globals.DarkGray, colour);

            Globals.SpriteBatch.DrawString(
                UISettings.fontUI,
                gun.Name,
                new Vector2(HUD_LEFT + BAR_WIDTH + 10, y - 4),
                ready ? gun.Colour : Globals.Gray,
                0f,
                Vector2.Zero,
                LABEL_SCALE,
                SpriteEffects.None,
                0f);

            y += BAR_HEIGHT + 4;
        }

        return y;
    }

    private void DrawBar(float y, float fill, Color background, Color foreground)
    {
        fill = MathHelper.Clamp(fill, 0f, 1f);

        Globals.SpriteBatch.Draw(_pixel, new Rectangle(HUD_LEFT, (int)y, BAR_WIDTH, BAR_HEIGHT), background);
        Globals.SpriteBatch.Draw(_pixel, new Rectangle(HUD_LEFT, (int)y, (int)(BAR_WIDTH * fill), BAR_HEIGHT), foreground);
    }

    // Short list of what is currently helping or hurting the player
    private static void DrawStatusLine(IPlayer player, float y)
    {
        List<string> statuses = [];

        // Worst first. Rooted means the controls are gone, which the player
        // needs told before anything else - he is about to think the game has
        // frozen otherwise.
        if (player.IsRooted)
            statuses.Add("STUCK");

        if (player.IsPoisoned)
            statuses.Add("POISONED");

        if (player.IsSlowed)
            statuses.Add("WEBBED");

        if (player.HasDoubleJump)
            statuses.Add(player.CanJumpAgain ? "JUMP x2" : "JUMP x2 (used)");

        if (statuses.Count == 0)
            return;

        Globals.SpriteBatch.DrawString(
            UISettings.fontUI,
            string.Join("   ", statuses),
            new Vector2(HUD_LEFT, y + 4),
            Globals.AlmostWhite
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
