using FMOD;
using System;

namespace SNEngine.Audio.Internal;

/// <summary>
/// Fire-and-forget sound effects player. Each PlaySE creates a new instance on the SE bus.
/// </summary>
internal sealed class SePlayer : ISePlayer
{
    private readonly IFmodCore _core;
    private readonly ISoundCache _cache;

    public SePlayer(IFmodCore core, ISoundCache cache)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public void Play(string assetName, float volume, float pitch)
    {
        if (!_core.System.HasValue || string.IsNullOrWhiteSpace(assetName))
            return;

        Sound sound = _cache.GetOrLoad(assetName, loop: false);
        if (!sound.hasHandle())
            return;

        RESULT res = _core.System.Value.playSound(sound, _core.SeGroup, false, out Channel ch);
        FmodCore.ERRCHECK(res);

        if (ch.hasHandle())
        {
            ch.setVolume(Math.Clamp(volume, 0f, 1f));
            if (Math.Abs(pitch - 1f) > 0.001f)
            {
                ch.setPitch(Math.Clamp(pitch, 0.1f, 10f));
            }
            // One-shot: FMOD recycles the channel automatically.
        }
    }

    public void StopAll()
    {
        if (_core.SeGroup.hasHandle())
            _core.SeGroup.stop();
    }
}
