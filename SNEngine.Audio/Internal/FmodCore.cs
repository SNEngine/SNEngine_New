using FMOD;
using System;

namespace SNEngine.Audio.Internal;

/// <summary>
/// Concrete implementation of low-level FMOD management.
/// </summary>
internal sealed class FmodCore : IFmodCore, IDisposable
{
    private FMOD.System? _system;

    public bool IsInitialized => _system.HasValue;
    public FMOD.System? System => _system;

    public ChannelGroup MasterGroup { get; private set; }
    public ChannelGroup BgmGroup { get; private set; }
    public ChannelGroup SeGroup { get; private set; }
    public ChannelGroup VoiceGroup { get; private set; }

    public void Initialize()
    {
        if (IsInitialized)
            return;

        RESULT res = Factory.System_Create(out var sys);
        ERRCHECK(res);
        _system = sys;

        res = _system.Value.init(512, INITFLAGS.NORMAL, IntPtr.Zero);
        ERRCHECK(res);

        res = _system.Value.getMasterChannelGroup(out var master);
        ERRCHECK(res);
        MasterGroup = master;

        BgmGroup = CreateAndAttachGroup("BGM");
        SeGroup = CreateAndAttachGroup("SE");
        VoiceGroup = CreateAndAttachGroup("Voice");

        SNEngine.Core.Debug.Log("[AudioSystem] FMOD Core initialized (version ~2.03).");
    }

    private ChannelGroup CreateAndAttachGroup(string name)
    {
        RESULT res = _system!.Value.createChannelGroup(name, out var group);
        ERRCHECK(res);
        res = MasterGroup.addGroup(group);
        ERRCHECK(res);
        return group;
    }

    public void Update()
    {
        if (!_system.HasValue)
            return;

        RESULT r = _system.Value.update();
        ERRCHECK(r);
    }

    public void ApplyBusVolumes(bool isMuted, float masterVol, float bgmVol, float seVol, float voiceVol)
    {
        if (!MasterGroup.hasHandle())
            return;

        MasterGroup.setVolume(isMuted ? 0f : masterVol);
        BgmGroup.setVolume(isMuted ? 0f : bgmVol);
        SeGroup.setVolume(isMuted ? 0f : seVol);
        VoiceGroup.setVolume(isMuted ? 0f : voiceVol);
    }

    public void SetPaused(bool paused)
    {
        if (MasterGroup.hasHandle())
            MasterGroup.setPaused(paused);
    }

    public void StopAllBuses()
    {
        if (BgmGroup.hasHandle()) BgmGroup.stop();
        if (SeGroup.hasHandle()) SeGroup.stop();
        if (VoiceGroup.hasHandle()) VoiceGroup.stop();
    }

    public void Shutdown()
    {
        if (_system.HasValue)
        {
            _system.Value.close();
            _system.Value.release();
            _system = null;
        }
    }

    public static void ERRCHECK(RESULT result)
    {
        if (result != RESULT.OK)
        {
            SNEngine.Core.Debug.LogError($"[FMOD] {result}: {Error.String(result)}");
        }
    }

    public void Dispose()
    {
        Shutdown();
    }
}
