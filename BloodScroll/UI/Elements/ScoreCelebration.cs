using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// HITTING A ROUND NUMBER
//
// The score in the corner is the only number the player is actually chasing,
// and for most of a run it does nothing but tick. This is the one time it gets
// to be the loudest thing on screen.
//
// TWO BEATS, AND THE SOUND IS THE POINT:
//
//   DRUMROLL  the number swells, accelerating, while the roll builds.
//             It grows DOWN AND LEFT - the score is pinned to the top right
//             corner, so that is the only direction with any room in it.
//   POP       the confetti goes off behind the number and the caption lands.
//             The number punches slightly past its full size and settles back.
//   SETTLE    it shrinks back to an ordinary HUD number and the run goes on.
//
// THE CLOCK BELONGS TO THE AUDIO, NOT THE OTHER WAY ROUND
//
// drumroll.wav is one recording holding both halves: 5.971s long, with the
// cymbal landing at 4.25s and ringing out for the 1.721s after it. So there is
// no second sound to fire at the pop - the crash is already sounding, and the
// only job here is to put the confetti on that exact frame.
//
// EVERY DURATION BELOW IS MEASURED OFF THAT FILE. Re-cut the wav and these
// three numbers have to move with it, or the confetti drifts off the cymbal.
//
// THE CONFETTI IS ONLY A PICTURE. It is drawn in screen space behind the score
// and touches nothing.
//
// USES ITS OWN Random AND MUST KEEP DOING SO. Globals.R is the seeded stream
// the world is built from and the order of draws off it is load bearing (see
// MobManager.SpawnOrder) - a milestone lands whenever the player happens to
// earn one, so taking even one number from it would change every layer above.
//

public class ScoreCelebration
{
    //
    // THE LANDMARKS
    //
    //   5,000, then 10,000, then 50,000, then 100,000
    //   then every 100,000 up to a million
    //   then every 500,000, forever
    //
    private const long FIRST = 5_000;
    private const long SECOND = 10_000;
    private const long THIRD = 50_000;
    private const long FOURTH = 100_000;
    private const long HUNDRED_THOUSAND = 100_000;
    private const long MILLION = 1_000_000;
    private const long HALF_MILLION = 500_000;

    // The next one worth stopping for. Everything below it has been celebrated
    // or deliberately skipped - see Update.
    private long nextMilestone = FIRST;

    private enum Phase { Idle, Drumroll, Pop, Settle }

    private Phase phase = Phase.Idle;
    private float timer = 0f;

    // What is being celebrated, for the caption
    private string caption = "";

    //
    // HOW LONG EACH BEAT LASTS, TAKEN OFF drumroll.wav
    //
    // The roll is where the cymbal is in the file. The number therefore stops
    // growing on the frame the crash sounds, which is the whole trick.
    //
    // The other two share the 1.721s of cymbal ringing out after it, so the
    // number has finished shrinking at about the moment the file goes quiet.
    //
    private const float DRUMROLL_SECONDS = 4.25f;
    private const float POP_SECONDS = 1.2f;
    private const float SETTLE_SECONDS = 0.52f;

    // How much bigger the number gets, as a multiple of its ordinary HUD size.
    // GamePlayUI holds it back further if the digits would reach the middle of
    // the screen - see ScoreScale there.
    private const float BIG = 3.6f;

    // The overshoot on the pop. Small: this is a number landing hard, not a
    // balloon.
    private const float PUNCH = 1.12f;

    //
    // WHAT THE HUD ASKS FOR
    //

    // 1 while nothing is happening, up to BIG at the peak. Multiplies whatever
    // scale the score is normally drawn at.
    public float Scale => phase switch
    {
        // Accelerating, so the growth is still speeding up when the pop lands
        Phase.Drumroll => MathHelper.Lerp(1f, BIG, Eased(timer / DRUMROLL_SECONDS)),

        // Lands past full size and eases back onto it
        Phase.Pop => BIG * MathHelper.Lerp(PUNCH, 1f, MathF.Min(1f, timer / (POP_SECONDS * 0.35f))),

        Phase.Settle => MathHelper.Lerp(BIG, 1f, timer / SETTLE_SECONDS),

        _ => 1f,
    };

    //
    // THE WORD "SCORE" BESIDE THE NUMBER, and whether it is still there
    //
    // GONE FOR THE WHOLE CELEBRATION. The number is the thing being celebrated
    // and a label hanging off it is just clutter at that size - and the label
    // sits where the number is about to be, so leaving it up means the two
    // overlap. Faded rather than switched off, so it does not blink.
    //
    private const float LABEL_FADE_SECONDS = 0.22f;

    public float ScoreLabelAlpha => phase switch
    {
        // Out quickly, as soon as the roll starts
        Phase.Drumroll => 1f - MathHelper.Clamp(timer / LABEL_FADE_SECONDS, 0f, 1f),

        Phase.Pop => 0f,

        // And back in over the tail of the shrink, so it has returned by the
        // time the number is its ordinary size again
        Phase.Settle => MathHelper.Clamp(
            (timer - (SETTLE_SECONDS - LABEL_FADE_SECONDS)) / LABEL_FADE_SECONDS, 0f, 1f),

        _ => 1f,
    };

    // The line under the number. Empty until the pop - the drumroll is meant to
    // be a question, and printing the answer through it gives the game away.
    public string Caption => phase == Phase.Pop || phase == Phase.Settle ? caption : "";

    public float CaptionAlpha => phase switch
    {
        Phase.Pop => MathF.Min(1f, timer / 0.12f),
        Phase.Settle => 1f - timer / SETTLE_SECONDS,
        _ => 0f,
    };

    // Squared: slow to start, quickest at the end
    private static float Eased(float t) => MathHelper.Clamp(t, 0f, 1f) * MathHelper.Clamp(t, 0f, 1f);

    //
    // A NEW RUN STARTS BACK AT THE BOTTOM
    //
    // Called from BloodScroll.Restart, because the score it is watching has
    // just been put back to zero and every landmark is there to be hit again.
    //
    // THE ROLL IS CUT OFF HERE TOO. It runs for six seconds, which is long
    // enough for a run to end underneath one - and a cymbal crash landing on
    // the death screen would be celebrating the wrong thing entirely.
    //
    public void Restart(IAudioService audio)
    {
        if (phase != Phase.Idle)
            audio.StopSound(AudioId.ScoreDrumroll);

        phase = Phase.Idle;
        timer = 0f;
        caption = "";
        nextMilestone = FIRST;
        confetti.Clear();
    }

    //
    // THE NEXT LANDMARK ABOVE A GIVEN SCORE
    //
    public static long MilestoneAfter(long score)
    {
        if (score < FIRST) return FIRST;
        if (score < SECOND) return SECOND;
        if (score < THIRD) return THIRD;
        if (score < FOURTH) return FOURTH;

        if (score < MILLION)
            return (score / HUNDRED_THOUSAND + 1) * HUNDRED_THOUSAND;

        return (score / HALF_MILLION + 1) * HALF_MILLION;
    }

    public void Update(IAudioService audio, Vector2 burstCentre)
    {
        UpdateConfetti();

        long score = Globals.POINTS;

        if (score >= nextMilestone)
            Begin(audio, score);

        if (phase == Phase.Idle)
            return;

        timer += Globals.DT;

        switch (phase)
        {
            case Phase.Drumroll when timer >= DRUMROLL_SECONDS:
                phase = Phase.Pop;
                timer = 0f;

                // NOTHING IS PLAYED HERE. The cymbal is 4.25s into the roll
                // that is already running, and DRUMROLL_SECONDS is that same
                // 4.25 - so the crash is sounding on this very frame and the
                // confetti only has to go off against it.
                Burst(burstCentre);
                break;

            case Phase.Pop when timer >= POP_SECONDS:
                phase = Phase.Settle;
                timer = 0f;
                break;

            case Phase.Settle when timer >= SETTLE_SECONDS:
                phase = Phase.Idle;
                timer = 0f;
                break;
        }
    }

    //
    // STARTING ONE, AND SKIPPING THE ONES THAT WENT PAST IN THE SAME INSTANT
    //
    // Beating the hive DOUBLES the score (see BossSchedule), which can vault
    // several landmarks at once. Celebrating each of them in turn would leave
    // the player watching a queue of drumrolls for a single kill, so only the
    // HIGHEST one crossed is announced and the rest are quietly banked.
    //
    // A celebration already running is not interrupted either - its own pop is
    // still to come, and cutting it off for a bigger number would throw away
    // the beat that was being built.
    //
    private void Begin(IAudioService audio, long score)
    {
        long reached = nextMilestone;

        while (MilestoneAfter(reached) <= score)
            reached = MilestoneAfter(reached);

        nextMilestone = MilestoneAfter(reached);

        if (phase != Phase.Idle)
            return;

        caption = Format(reached) + "!";

        phase = Phase.Drumroll;
        timer = 0f;

        audio.PlaySound(AudioId.ScoreDrumroll);
    }

    //
    // HOW A LANDMARK READS
    //
    // IN WORDS, ALL THE WAY UP: "10 THOUSAND", "300 THOUSAND", "1 MILLION",
    // "2.5 MILLION". Never the digits - the digits are already on screen in
    // letters four times their usual size directly above this line, and
    // printing them twice says nothing the second time.
    //
    // EVERY LANDMARK DIVIDES CLEANLY, which is what makes this safe. Below a
    // million they are all multiples of ten thousand, so the thousands count is
    // always whole; above it they are all multiples of half a million, so the
    // only fraction that can ever appear is .5.
    //
    // INVARIANT CULTURE for that .5: this machine writes a decimal point as a
    // comma, and "1,5 MILLION" is not what anybody means.
    //
    private static string Format(long milestone)
    {
        if (milestone < MILLION)
            return (milestone / 1_000).ToString(CultureInfo.InvariantCulture) + " THOUSAND";

        double millions = milestone / (double)MILLION;

        string number = millions == Math.Floor(millions)
            ? ((long)millions).ToString(CultureInfo.InvariantCulture)
            : millions.ToString("0.0", CultureInfo.InvariantCulture);

        return number + " MILLION";
    }

    //
    // THE CONFETTI
    //

    private const int PIECES = 150;

    // Thrown hard and then dragged down. A wide band, so the burst has a dense
    // heart and a thin scatter well past it rather than one clean ring.
    private const float SPEED_MIN = 260f;
    private const float SPEED_MAX = 1150f;

    // What is left of a piece's speed after a second
    private const float DRAG_PER_SECOND = 0.06f;

    // Downwards is positive. Confetti FALLS - it is the one thing here that
    // separates it from the butterfly's dust, which rises.
    private const float FALL = 1400f;

    private const float LIFE_MIN = 1.1f;
    private const float LIFE_MAX = 1.9f;

    private const int LONG_MIN = 9;
    private const int LONG_MAX = 20;
    private const int SHORT_MIN = 3;
    private const int SHORT_MAX = 7;

    // How fast a piece tumbles, in radians a second
    private const float SPIN_MAX = 14f;

    // The game's own palette rather than a party bag of new colours - every one
    // of these is already worn by something on screen, so the burst belongs to
    // this game and not to a stock effect.
    private static readonly Color[] Palette =
    [
        Globals.Yellow,
        Globals.HotPink,
        Globals.PistolBlue,
        Globals.HealGreen,
        Globals.BlastOrange,
        Globals.StunPurple,
        Globals.AlmostWhite,
    ];

    private struct Confetto
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float Spin;
        public Color Colour;
        public Vector2 Size;

        // Its own share of the burst's life, so the cloud thins out unevenly
        // instead of every piece switching off on the same frame
        public float Life;
        public float Age;
    }

    private readonly List<Confetto> confetti = [];

    // NOT Globals.R - see the note at the top of the file
    private static readonly Random rng = new();

    private void Burst(Vector2 centre)
    {
        for (int i = 0; i < PIECES; i++)
        {
            float angle = (float)rng.NextDouble() * MathF.Tau;
            float speed = MathHelper.Lerp(SPEED_MIN, SPEED_MAX, (float)rng.NextDouble());

            confetti.Add(new Confetto
            {
                Position = centre,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Rotation = (float)rng.NextDouble() * MathF.Tau,
                Spin = ((float)rng.NextDouble() * 2f - 1f) * SPIN_MAX,
                Colour = Palette[rng.Next(Palette.Length)],
                Size = new Vector2(
                    rng.Next(LONG_MIN, LONG_MAX + 1),
                    rng.Next(SHORT_MIN, SHORT_MAX + 1)),
                Life = MathHelper.Lerp(LIFE_MIN, LIFE_MAX, (float)rng.NextDouble()),
                Age = 0f,
            });
        }
    }

    // Runs whatever the number is doing, so the last pieces are still falling
    // after it has shrunk back
    private void UpdateConfetti()
    {
        if (confetti.Count == 0)
            return;

        float drag = MathF.Pow(DRAG_PER_SECOND, Globals.DT);

        for (int i = confetti.Count - 1; i >= 0; i--)
        {
            Confetto piece = confetti[i];

            piece.Age += Globals.DT;

            if (piece.Age >= piece.Life)
            {
                confetti.RemoveAt(i);
                continue;
            }

            piece.Velocity *= drag;
            piece.Velocity.Y += FALL * Globals.DT;
            piece.Position += piece.Velocity * Globals.DT;
            piece.Rotation += piece.Spin * Globals.DT;

            confetti[i] = piece;
        }
    }

    //
    // Drawn BEFORE the number, so the score stays readable through its own
    // celebration. RoundedRect owns the one white pixel every flat shape in the
    // UI is stretched from, and stretching it unevenly and turning it is what
    // makes a tumbling paper rectangle.
    //
    public void DrawConfetti()
    {
        foreach (Confetto piece in confetti)
        {
            float t = piece.Age / piece.Life;

            // Holds its colour and then goes quickly, so the burst reads as
            // confetti for most of its life rather than as a grey smear
            float alpha = 1f - t * t * t;

            Globals.SpriteBatch.Draw(
                RoundedRect.Pixel,
                piece.Position,
                null,
                piece.Colour * alpha,
                piece.Rotation,
                new Vector2(0.5f, 0.5f),
                piece.Size,
                SpriteEffects.None,
                0f);
        }
    }
}
