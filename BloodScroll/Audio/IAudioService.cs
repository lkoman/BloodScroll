public interface IAudioService
{
    void PlaySound(AudioId id);
    void StopSound(AudioId id);
    void PlayMusic(AudioId musicId, bool loop = true);
    void StopMusic();
    void SetMasterVolume(float volume); // 0..1
    float GetMasterVolume();
    void SwitchToMenuMusic();
    void SwitchToGameMusic();
    void SwitchToBossMusic();
}
