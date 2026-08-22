using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE IN GAME HUD
//
// Three things pinned to the top of the screen and one to the bottom: what is
// keeping you alive on the left, where you are in the middle, what it has been
// worth on the right, and what E does right now along the bottom.
//
// NOTHING HERE HAS A BACKGROUND BEHIND IT. The menus are panels because they
// are the only thing on the screen; the HUD is not, and a dark plate in each
// corner of a game you are trying to look at is four holes punched in the
// playfield. A bat crossing behind the health bar has to stay visible.
//
// So legibility is bought some other way. Every string on the HUD is plain
// white with a solid black edge laid all the way round it, which holds a word
// together over any background the generator comes up with - and white is the
// one colour that never collides with the game behind it, because the game is
// made of reds and greens and the sea. Colour still does the signalling here,
// but it does it in the bars and the tags, never in a letterform. The bars
// keep a dark track, because a bar with nothing behind it cannot show how empty
// it is - but the track is only as wide as the bar and nothing else is filled
// in at all.
//

public class GamePlayUI
{
    private string LevelString;

    private const int HUD_LEFT = 40;
    private const int HUD_TOP = 36;

    private const int BAR_WIDTH = 300;
    private const int BAR_HEIGHT = 16;
    private const int BAR_GAP = 7;

    // Between a bar and the word beside it saying what it is
    private const int LABEL_GAP = 12;

    // The bar names, and the HP figure over the top of them. Small - the colour
    // is what is actually being read, the word is only there for the first time
    // you see it.
    private const float LABEL_SCALE = 0.5f;
    private const float VALUE_SCALE = 0.62f;

    private const int VALUE_TO_BARS = 8;

    // The status tags under the bars. These DO keep an outline and a wash of
    // colour behind them - they are the one part of the HUD that has to be
    // caught out of the corner of the eye mid fight, and a tag is small enough
    // that nothing meaningful is hidden under it.
    private const int CHIP_HEIGHT = 26;
    private const int CHIP_PAD_X = 12;
    private const int CHIP_GAP = 8;
    private const int BARS_TO_CHIPS = 10;
    private const float CHIP_SCALE = 0.46f;

    // The one tag that is not about the game. Named once so the row that adds
    // it and the switch that colours it can never drift apart.
    private const string DEBUG_TAG = "DEBUG";

    //
    // WHAT E DOES RIGHT NOW
    //
    // Along the bottom middle of the screen rather than over the flower or over
    // the player's head: this is HUD, drawn in screen space, so it sits in the
    // same place every time and does not go chasing a swinging flower head or
    // wander off the top of the screen when the player is standing high up.
    //
    private const string PLUCK_TEXT = "PICK UP";
    private const string PLANT_TEXT = "PLANT THE BOMB";

    // How far the bottom of the prompt sits above the bottom edge
    private const int PROMPT_BOTTOM = 90;
    private const int PROMPT_HEIGHT = 56;
    private const int KEYCAP = 34;

    // Latched by the flower the player is standing on while the world updates,
    // and cleared the moment it has been drawn - so the offer lives exactly one
    // frame and never shows for a frame in which no flower made it.
    private static bool pluckOffered = false;

    // Called by Flower while the player is in reach of pulling it up
    public static void OfferPluck() => pluckOffered = true;

    // Nothing to build any more - RoundedRect owns the one white pixel the bars
    // are stretched from and bakes its own corners the first time it is asked
    public void LoadContent(GraphicsDevice device) { }

    public void Draw(IPlayer player, IWeaponsManager weapons)
    {
        DrawVitals(player, weapons);
        DrawLayerName();
        DrawScore();
        DrawInteractPrompt(player);
    }

    //
    // THE TOP LEFT CORNER: HP, SHIELD, GUNS, STATUS
    //
    private void DrawVitals(IPlayer player, IWeaponsManager weapons)
    {
        List<string> statuses = Statuses(player);

        float valueHeight = UISettings.fontUI.LineSpacing * VALUE_SCALE;

        float x = HUD_LEFT;
        float y = HUD_TOP;

        // The figure over the bars
        UITheme.DrawTextOutlined(UISettings.fontUI, "HP", new Vector2(x, y), UITheme.TextBright, VALUE_SCALE);

        string hp = player.HP + " / " + player.MaxHP;
        float hpWidth = UISettings.fontUI.MeasureString(hp).X * VALUE_SCALE;

        UITheme.DrawTextOutlined(UISettings.fontUI, hp,
            new Vector2(x + BAR_WIDTH - hpWidth, y),
            UITheme.TextBright,
            VALUE_SCALE);

        y += valueHeight + VALUE_TO_BARS;

        DrawBar(x, y, player.HP / (float)player.MaxHP, Globals.Red, null);
        y += BAR_HEIGHT + BAR_GAP;

        if (player.ShieldMax > 0)
        {
            // Same yellow as the bubble around the player, so the bar and the
            // ring are obviously the same thing
            DrawBar(x, y, player.Shield / (float)player.ShieldMax, Globals.Yellow, "SHIELD");
            y += BAR_HEIGHT + BAR_GAP;
        }

        y = DrawWeaponCooldowns(weapons, x, y);

        if (statuses.Count > 0)
            DrawStatusChips(player, statuses, x, y - BAR_GAP + BARS_TO_CHIPS);
    }

    //
    // ONE BAR PER SLOW GUN, under the health and the shield
    //
    // Stacked in the order they are unlocked, so the stun sits under the shield
    // and the shell sits under the stun and neither ever moves once it appears.
    // Each bar is drawn in ITS OWN GUN'S COLOUR, the same colour as the gun in
    // the player's hands and the shots coming out of it - there is no label to
    // read in the middle of a fight, so the colour has to be the label.
    //
    // A full bar means the gun is ready. It fills as the gun recharges rather
    // than draining, because the thing worth glancing at is whether it is up.
    //
    private float DrawWeaponCooldowns(IWeaponsManager weapons, float x, float y)
    {
        if (weapons == null)
            return y;

        foreach (WeaponCooldown gun in weapons.Cooldowns)
        {
            // Dimmed while it is still filling, full brightness the instant it
            // is ready - the bar finishing is a thing you can catch out of the
            // corner of your eye
            bool ready = gun.Ready >= 1f;

            DrawBar(x, y, gun.Ready, ready ? gun.Colour : gun.Colour * 0.65f, gun.Name);

            y += BAR_HEIGHT + BAR_GAP;
        }

        return y;
    }

    //
    // ONE BAR: a sunken track, the fill, and a highlight along the top of the
    // fill so it reads as a lit strip rather than a block of flat colour
    //
    private void DrawBar(float x, float y, float fill, Color colour, string label)
    {
        fill = MathHelper.Clamp(fill, 0f, 1f);

        Rectangle track = new((int)x, (int)y, BAR_WIDTH, BAR_HEIGHT);

        RoundedRect.Fill(track, UITheme.Shadow * 0.55f, UITheme.RadiusBar);
        RoundedRect.Border(track, Color.White * 0.07f, UITheme.RadiusBar, 1);

        int width = (int)(BAR_WIDTH * fill);

        // Below this there is not enough room left for a rounded cap and the
        // bar turns into a smear, so it is simply not drawn
        if (width >= UITheme.RadiusBar * 2)
        {
            Rectangle filled = new((int)x, (int)y, width, BAR_HEIGHT);

            RoundedRect.Fill(filled, colour, UITheme.RadiusBar);

            RoundedRect.Rect(
                new Rectangle(filled.X + UITheme.RadiusBar, filled.Y + 2,
                              filled.Width - UITheme.RadiusBar * 2, 2),
                Color.White * 0.22f);
        }

        if (label == null)
            return;

        // The word beside a bar is white like everything else on the HUD - the
        // bar it is sitting next to is already drawn in the gun's own colour,
        // so the colour coding survives without staking a word's legibility on
        // a dim grey or a mid green being readable over the playfield
        UITheme.DrawTextOutlined(UISettings.fontUI, label,
            new Vector2(x + BAR_WIDTH + LABEL_GAP, y - 1),
            UITheme.TextBright,
            LABEL_SCALE);
    }

    // Short list of what is currently helping or hurting the player
    private static List<string> Statuses(IPlayer player)
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

        // LAST, and in a colour nothing else in the game wears. A run played
        // with this on is not a real run, and the one way that goes wrong is
        // forgetting it is on - so it has to be on screen for as long as it is.
        if (DebugMode.Invulnerable)
            statuses.Add(DEBUG_TAG);

        return statuses;
    }

    //
    // THE STATUSES, AS COLOURED TAGS
    //
    // A run of words all in the same white was a sentence you had to read. Each
    // one wears the colour of the thing that caused it - the same green as the
    // bat that poisoned you, the same red as the web holding you - so the row
    // is glanceable and a bad status is red whether or not the word is read.
    //
    private static void DrawStatusChips(IPlayer player, List<string> statuses, float x, float y)
    {
        foreach (string status in statuses)
        {
            Color colour = StatusColour(status, player);

            float textWidth = UISettings.fontUI.MeasureString(status).X * CHIP_SCALE;
            Rectangle chip = new((int)x, (int)y, (int)textWidth + CHIP_PAD_X * 2, CHIP_HEIGHT);

            // The tag keeps its colour, the word inside it does not: the wash
            // and the outline are what is being glanced at, and the word only
            // has to be readable once
            RoundedRect.Fill(chip, colour * 0.22f, UITheme.RadiusChip);
            RoundedRect.Border(chip, colour * 0.75f, UITheme.RadiusChip, 1);

            UITheme.DrawTextOutlined(UISettings.fontUI, status,
                new Vector2(chip.X + CHIP_PAD_X,
                            chip.Y + chip.Height / 2f - UISettings.fontUI.LineSpacing * CHIP_SCALE / 2f),
                UITheme.TextBright, CHIP_SCALE);

            x += chip.Width + CHIP_GAP;
        }
    }

    private static Color StatusColour(string status, IPlayer player)
    {
        // Hot pink, which is used nowhere else on the HUD and by nothing in the
        // world. It is meant to look like it does not belong there, because it
        // does not.
        if (status == DEBUG_TAG)
            return Globals.HotPink;

        if (status == "STUCK")
            return Globals.Red;

        if (status == "POISONED")
            return Globals.PoisonGreen;

        if (status == "WEBBED")
            return Globals.Gray;

        // The double jump is the only good one on the list, and it goes grey
        // the moment it has been spent
        return player.CanJumpAgain ? Globals.HealGreen : Globals.Gray;
    }

    //
    // WHERE YOU ARE, in the middle of the top edge
    //
    private void DrawLayerName()
    {
        // Every font in the game is built over plain ASCII only, so the
        // separator has to be something that exists in it - a middle dot or an
        // em dash would throw the moment the HUD tried to measure it
        bool boss = Globals.CurrentLayerType == "Boss Layer";

        // Upper cased to match the other two plates. The layer types are
        // written in title case where they are defined, which put a "Ground
        // Layer" next to a "LAYER 0" and read as two different labels.
        LevelString = boss
            ? Globals.CurrentLayerType.ToUpper()
            : Globals.CurrentLayerType.ToUpper() + "   |   LAYER " + Globals.CurrentLayerIndex;

        // A boss layer is the one you are not allowed to miss. It used to say so
        // in red, which is the colour of half the things already on the screen -
        // the blood, the health bar, the fire - and a red word at the top of a
        // red fight is the first thing to disappear. It is white like the rest
        // of the HUD now, and the underline the ordinary layers do not get is
        // what marks it out.
        UITheme.DrawTextCentredOutlined(UISettings.fontUI, LevelString,
            Globals.VIRTUAL_WIDTH / 2f,
            HUD_TOP,
            UITheme.TextBright,
            VALUE_SCALE);

        if (!boss)
            return;

        float width = UISettings.fontUI.MeasureString(LevelString).X * VALUE_SCALE;

        RoundedRect.Fill(
            new Rectangle(
                (int)(Globals.VIRTUAL_WIDTH / 2f - width / 2f),
                (int)(HUD_TOP + UISettings.fontUI.LineSpacing * VALUE_SCALE + 2),
                (int)width,
                3),
            UITheme.TextBright, 1);
    }

    //
    // WHAT IT HAS BEEN WORTH, in the top right
    //
    private static void DrawScore()
    {
        string points = Globals.POINTS.ToString();

        float labelWidth = UISettings.fontUI.MeasureString("SCORE").X * LABEL_SCALE;
        float valueWidth = UISettings.fontUI.MeasureString(points).X * VALUE_SCALE;

        // Both pinned to the right edge and sharing a middle line, so the word
        // stays put while the number grows leftwards under it
        float right = Globals.VIRTUAL_WIDTH - HUD_LEFT;
        float centreY = HUD_TOP + UISettings.fontUI.LineSpacing * VALUE_SCALE / 2f;

        UITheme.DrawTextOutlined(UISettings.fontUI, "SCORE",
            new Vector2(right - valueWidth - LABEL_GAP - labelWidth,
                        centreY - UISettings.fontUI.LineSpacing * LABEL_SCALE / 2f),
            UITheme.TextBright, LABEL_SCALE);

        UITheme.DrawTextOutlined(UISettings.fontUI, points,
            new Vector2(right - valueWidth,
                        centreY - UISettings.fontUI.LineSpacing * VALUE_SCALE / 2f),
            UITheme.TextBright, VALUE_SCALE);
    }

    //
    // THE E PROMPT
    //
    // Carrying a bomb wins over standing on a flower, because the world takes
    // the key press before any flower gets a look at it - a player with full
    // hands always puts down what he is holding, so that is what it must say.
    //
    // The key itself is drawn as a key rather than written into the sentence:
    // the one thing the player has to find is which button to press, and a
    // little square with E in it is found without reading anything.
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

        float textWidth = UISettings.fontUI.MeasureString(text).X * VALUE_SCALE;
        int width = (int)(KEYCAP + LABEL_GAP + textWidth);

        Rectangle prompt = new(
            (int)(Globals.VIRTUAL_WIDTH / 2f - width / 2f),
            Globals.VIRTUAL_HEIGHT - PROMPT_BOTTOM - PROMPT_HEIGHT,
            width,
            PROMPT_HEIGHT);

        Rectangle cap = new(
            prompt.X,
            prompt.Y + prompt.Height / 2 - KEYCAP / 2,
            KEYCAP,
            KEYCAP);

        RoundedRect.FillGradient(cap, UITheme.ButtonTop, UITheme.ButtonBottom, 8);
        RoundedRect.Border(cap, UITheme.AccentBright, 8, 1);

        UITheme.DrawTextCentredOutlined(UISettings.fontUI, "E",
            cap.X + cap.Width / 2f,
            cap.Y + cap.Height / 2f - UISettings.fontUI.LineSpacing * VALUE_SCALE / 2f,
            UITheme.TextBright, VALUE_SCALE);

        UITheme.DrawTextOutlined(UISettings.fontUI, text,
            new Vector2(cap.Right + LABEL_GAP,
                        prompt.Y + prompt.Height / 2f - UISettings.fontUI.LineSpacing * VALUE_SCALE / 2f),
            UITheme.TextBright, VALUE_SCALE);
    }

    //
    // THE BOSS'S REMAINING HP, floated over the boss itself
    //
    // It moves with the thing it belongs to, and the number has to stay
    // attached to it rather than to a corner of the screen.
    //
    public static void DrawBossHP(int HP, float x, float y)
    {
        // White with the same black edge as the rest of the HUD. This number
        // floats over a boss, which is the busiest, brightest, most crowded
        // patch of the screen there is - it needs the outline more than
        // anything else drawn here.
        UITheme.DrawTextCentredOutlined(UISettings.fontUI, HP.ToString(), x, y,
            UITheme.TextBright, VALUE_SCALE);
    }
}
