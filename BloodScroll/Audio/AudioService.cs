using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed class AudioService : IAudioService
{
    private readonly Dictionary<AudioId, SoundEffect> sounds = [];
    private readonly Dictionary<AudioId, Song> music = [];

    public AudioService(ContentManager content)
    {
        // MENU
        sounds[AudioId.ButtonHover] = content.Load<SoundEffect>("Audio/button-hover");
        sounds[AudioId.ButtonClick] = content.Load<SoundEffect>("Audio/button-click");

        // PLAYER
        sounds[AudioId.PlayerGun] = content.Load<SoundEffect>("Audio/player-gun");
        sounds[AudioId.PlayerJump] = content.Load<SoundEffect>("Audio/player-jump");
        sounds[AudioId.PlayerHit] = content.Load<SoundEffect>("Audio/player-hit");

        // MONSTERS
        sounds[AudioId.BatSqueak] = content.Load<SoundEffect>("Audio/bat-squeak");

        // Bosses
        sounds[AudioId.FireHit] = content.Load<SoundEffect>("Audio/fire-hit");

        // MUSIC
        music[AudioId.MenuMusic] = content.Load<Song>("Audio/menu-music");
        music[AudioId.GameMusic] = content.Load<Song>("Audio/game-music");
        music[AudioId.BossMusic] = content.Load<Song>("Audio/boss-music");
    }

    public void PlaySound(AudioId id)
    {
        if (sounds.TryGetValue(id, out var s))
            s.Play();
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
