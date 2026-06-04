using FMOD;

namespace SNEngine.Audio.Internal;

/// <summary>
/// Abstraction over the low-level FMOD system and bus groups.
/// Allows the audio players to remain decoupled from concrete FMOD implementation.
/// </summary>
internal interface IFmodCore
{
    bool IsInitialized { get; }
    FMOD.System? System { get; }

    ChannelGroup MasterGroup { get; }
    ChannelGroup BgmGroup { get; }
    ChannelGroup SeGroup { get; }
    ChannelGroup VoiceGroup { get; }

    void Initialize();
    void Update();
    void ApplyBusVolumes(bool isMuted, float masterVol, float bgmVol, float seVol, float voiceVol);
    void SetPaused(bool paused);
    void StopAllBuses();
    void Shutdown();
}
