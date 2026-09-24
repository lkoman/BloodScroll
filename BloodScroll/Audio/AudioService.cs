using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed class AudioService : IAudioService
{
    private readonly Dictionary<AudioId, SoundEffectInstance> sounds = [];
    private readonly Dictionary<AudioId, Song> music = [];

    // How loud the score drumroll sits against everything else. The one number
    // to move if the celebration is still shouting over the fight.
    private const float SCORE_DRUMROLL_VOLUME = 0.35f;

    public AudioService(ContentManager content)
    {
        // MENU
        sounds[AudioId.ButtonHover] = content.Load<SoundEffect>("Audio/button-hover").CreateInstance();
        sounds[AudioId.ButtonClick] = content.Load<SoundEffect>("Audio/button-click").CreateInstance();

        // PLAYER
        sounds[AudioId.PlayerGun] = content.Load<SoundEffect>("Audio/player-gun").CreateInstance();
        sounds[AudioId.PlayerJump] = content.Load<SoundEffect>("Audio/player-jump").CreateInstance();
        sounds[AudioId.PlayerHit] = content.Load<SoundEffect>("Audio/player-hit").CreateInstance();

        // THE SWORD IS SILENT until there is a wosh to play. PlaySound only
        // plays ids it has been given, so the swing asks for one every time and
        // quietly gets nothing - dropping the file into Content and taking the
        // comment off the line below is the whole job.
        //sounds[AudioId.SwordSwing] = content.Load<SoundEffect>("Audio/sword-swing").CreateInstance();

        // MONSTERS
        sounds[AudioId.BatSqueak] = content.Load<SoundEffect>("Audio/bat-squeak").CreateInstance();

        // Bosses
        sounds[AudioId.FireHit] = content.Load<SoundEffect>("Audio/fire-hit").CreateInstance();

        // HITTING A ROUND SCORE. The roll and the cymbal that ends it are the
        // same recording - ScoreCelebration is timed to the file rather than
        // the file being cut to the animation.
        //
        // HELD WELL BACK. It is the longest and brashest thing in the game and
        // it plays over a fight that is still going on, so at full volume it
        // buries the gunfire the player is steering by. Volume here multiplies
        // with the master, so muting the game still silences it.
        var drumroll = content.Load<SoundEffect>("Audio/drumroll").CreateInstance();
        drumroll.Volume = SCORE_DRUMROLL_VOLUME;

        sounds[AudioId.ScoreDrumroll] = drumroll;

        // MUSIC
        music[AudioId.MenuMusic] = content.Load<Song>("Audio/menu-music");
        music[AudioId.GameMusic] = content.Load<Song>("Audio/game-music");
        music[AudioId.BossMusic] = content.Load<Song>("Audio/boss-music");
    }

    public void PlaySound(AudioId id)
    {
        if (sounds.TryGetValue(id, out var s))
        {
            if (s.State != SoundState.Playing)
            {
                s.Play();
            }
        }
    }

    //
    // CUTTING ONE SHORT
    //
    // For sounds long enough that the run can end underneath them - the score
    // drumroll is six seconds, so dying halfway through one would otherwise
    // land its cymbal crash on the death screen.
    //
    public void StopSound(AudioId id)
    {
        if (sounds.TryGetValue(id, out var s))
            s.Stop();
    }

    public void PlayMusic(AudioId musicId, bool loop = true)
    {
        if (music.TryGetValue(musicId, out var m))
        {
            MediaPlayer.IsRepeating = loop;
            MediaPlayer.Play(m);
        }
    }

    public void SwitchToMenuMusic()
    {
        StopMusic();
        PlayMusic(AudioId.MenuMusic);
    }

    public void SwitchToGameMusic()
    {
        StopMusic();
        PlayMusic(AudioId.GameMusic);
    }

    public void SwitchToBossMusic()
    {
        StopMusic();
        PlayMusic(AudioId.BossMusic);
    }

    public void StopMusic() => MediaPlayer.Stop();

    public void SetMasterVolume(float volume)
    {
        SoundEffect.MasterVolume = MathHelper.Clamp(volume, 0f, 1f);
        MediaPlayer.Volume = MathHelper.Clamp(volume, 0f, 1f);
    }

    public float GetMasterVolume()
    {
        return SoundEffect.MasterVolume;
    }
}
