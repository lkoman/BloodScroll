using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE IN GAME HUD
//
// Top left: HP, shield, gun cooldowns, status tags. Top middle: which layer.
// Top right: score. Bottom middle: what F does.
//
// NOTHING HERE HAS A BACKGROUND - the HUD sits over the playfield, so every
// string is white with a black outline all the way round (UITheme.
// DrawTextOutlined) instead of a dark plate. Colour signals in the bars and
// tags, never in the text. Only the bars keep a dark track.
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

    // THE WIDEST THE SCORE MAY EVER GET while a landmark is being celebrated.
    // Short of half the screen, so even a seven figure number swelling out of
    // the top right corner stops before it reaches the layer plate in the
    // middle. See ScoreScale.
    private const float MAX_SCORE_WIDTH = Globals.VIRTUAL_WIDTH * 0.44f;

    // The landmark caption under the number, and the air above it
    private const float CAPTION_SCALE = 0.9f;
    private const int CAPTION_GAP = 6;

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

    // WHAT F DOES RIGHT NOW. Bottom middle, in SCREEN space - not over the
    // flower or the player, so it never chases a swinging head off the screen.
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

    // NOTHING TO LOAD. RoundedRect owns the one white pixel the bars are
    // stretched from and bakes its own corners the first time it is asked, so
    // this class had an empty LoadContent and no longer has one at all.

    // HITTING A ROUND NUMBER. Owns the milestone clock and the confetti; the
    // HUD only asks it how big the score should be right now.
    private readonly ScoreCelebration celebration = new();

    //
    // TICKED EVERY FRAME, INCLUDING WHILE THE GAME IS PAUSED
    //
    // Beating the hive doubles the score and puts the gift card up, and the
    // card pauses the game - so the biggest milestone in a run lands on a
    // frame where nothing else is moving. UI.Update calls this outside the
    // which-screen-has-the-mouse chain for exactly that reason.
    //
    public void Update(IAudioService audio)
    {
        celebration.Update(audio, ScoreCentre(ScoreScale()));
    }

    // A fresh run starts back at the first landmark, and cuts off any roll
    // still playing over the run that just ended
    public void Restart(IAudioService audio) => celebration.Restart(audio);

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

        // Same dim-while-filling rule as the gun bars
        Color dash = player.DashReady >= 1f ? UITheme.AccentBright : UITheme.AccentBright * 0.65f;
        DrawBar(x, y, player.DashReady, dash, "DASH");
        y += BAR_HEIGHT + BAR_GAP;

        y = DrawWeaponCooldowns(weapons, x, y);

        if (statuses.Count > 0)
            DrawStatusChips(player, statuses, x, y - BAR_GAP + BARS_TO_CHIPS);
    }

    //
    // ONE BAR PER SLOW GUN, under the health and the shield
    //
    // Stacked in unlock order, so no bar moves once it appears. Each is drawn in
    // ITS OWN GUN'S COLOUR - the same colour as the gun and its shots.
    //
    // The bar FILLS as the gun recharges. Full means ready.
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

    // THE STATUSES, AS COLOURED TAGS. Each wears the colour of what caused it -
    // see StatusColour.
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

        // A boss layer is marked by the UNDERLINE, not by colour - the text
        // stays white like the rest of the HUD.
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
    // The digits, as the HUD prints them. No separators - the number is read as
    // a shape that keeps climbing, not parsed. The caption under a landmark is
    // the one place it gets written out properly (ScoreCelebration.Format).
    private static string Points() => Globals.POINTS.ToString();

    //
    // HOW BIG THE NUMBER IS RIGHT NOW
    //
    // Its ordinary HUD size, swollen by whatever the celebration is doing, and
    // then held back so the digits can never reach the layer plate in the
    // middle of the screen - a seven figure score at full size is wide, and
    // "grows down and left" stops being an answer once it has crossed the
    // middle. Only ever shrinks the wanted size, never adds to it.
    //
    private float ScoreScale()
    {
        float wanted = VALUE_SCALE * celebration.Scale;

        float width = UISettings.fontUI.MeasureString(Points()).X;

        if (width <= 0f)
            return wanted;

        return MathF.Min(wanted, MAX_SCORE_WIDTH / width);
    }

    //
    // THE MIDDLE OF THE NUMBER, in screen space, at a given size.
    //
    // The RIGHT EDGE and the TOP are the two pinned points - that is the whole
    // reason the number grows down and to the left as it swells, and never off
    // the corner it lives in.
    //
    private static Vector2 ScoreCentre(float scale)
    {
        float width = UISettings.fontUI.MeasureString(Points()).X * scale;
        float height = UISettings.fontUI.LineSpacing * scale;

        return new Vector2(
            Globals.VIRTUAL_WIDTH - HUD_LEFT - width / 2f,
            HUD_TOP + height / 2f);
    }

    private void DrawScore()
    {
        string points = Points();

        float scale = ScoreScale();
        float right = Globals.VIRTUAL_WIDTH - HUD_LEFT;

        float valueWidth = UISettings.fontUI.MeasureString(points).X * scale;
        float valueHeight = UISettings.fontUI.LineSpacing * scale;

        // BEHIND THE NUMBER, so the score stays readable through its own
        // celebration. Nothing is drawn at all unless a burst is in the air.
        celebration.DrawConfetti();

        //
        // THE WORD "SCORE"
        //
        // Placed where it sits at the ORDINARY size and left there, rather than
        // being pushed along by the number - a label sliding across the screen
        // ahead of a growing figure reads as a bug.
        //
        // It is not there at all during a celebration. The swelling number
        // takes the space the label was standing in, and nothing that big needs
        // telling the player what it is.
        //
        float restingWidth = UISettings.fontUI.MeasureString(points).X * VALUE_SCALE;
        float labelWidth = UISettings.fontUI.MeasureString("SCORE").X * LABEL_SCALE;
        float restingCentreY = HUD_TOP + UISettings.fontUI.LineSpacing * VALUE_SCALE / 2f;

        float labelAlpha = celebration.ScoreLabelAlpha;

        if (labelAlpha > 0.01f)
        {
            UITheme.DrawTextOutlined(UISettings.fontUI, "SCORE",
                new Vector2(right - restingWidth - LABEL_GAP - labelWidth,
                            restingCentreY - UISettings.fontUI.LineSpacing * LABEL_SCALE / 2f),
                UITheme.TextBright * labelAlpha, LABEL_SCALE);
        }

        // Top left corner of the number. Right edge and top both pinned.
        UITheme.DrawTextOutlined(UISettings.fontUI, points,
            new Vector2(right - valueWidth, HUD_TOP),
            UITheme.TextBright, scale);

        DrawMilestoneCaption(right, HUD_TOP + valueHeight);
    }

    //
    // WHAT THE LANDMARK WAS
    //
    // Under the number and pinned to the same right edge, in the gold nothing
    // else in the HUD wears - the bars and tags carry every other colour the
    // game uses, and this has to not be read as one of them.
    //
    private void DrawMilestoneCaption(float right, float top)
    {
        string caption = celebration.Caption;

        if (caption.Length == 0)
            return;

        float alpha = celebration.CaptionAlpha;

        if (alpha <= 0.01f)
            return;

        float width = UISettings.fontUI.MeasureString(caption).X * CAPTION_SCALE;

        UITheme.DrawTextOutlined(UISettings.fontUI, caption,
            new Vector2(right - width, top + CAPTION_GAP),
            Globals.Yellow * alpha, CAPTION_SCALE);
    }

    //
    // THE F PROMPT
    //
    // Carrying a bomb WINS over standing on a flower - the world takes the key
    // press before any flower sees it (GameWorld.UpdateBombs), so the prompt has
    // to say the same.
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

        UITheme.DrawTextCentredOutlined(UISettings.fontUI, "F",
            cap.X + cap.Width / 2f,
            cap.Y + cap.Height / 2f - UISettings.fontUI.LineSpacing * VALUE_SCALE / 2f,
            UITheme.TextBright, VALUE_SCALE);

        UITheme.DrawTextOutlined(UISettings.fontUI, text,
            new Vector2(cap.Right + LABEL_GAP,
                        prompt.Y + prompt.Height / 2f - UISettings.fontUI.LineSpacing * VALUE_SCALE / 2f),
            UITheme.TextBright, VALUE_SCALE);
    }

    // THE BOSS'S REMAINING HP, floated over the boss and moving with it
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
