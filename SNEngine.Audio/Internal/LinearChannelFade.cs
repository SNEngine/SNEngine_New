using FMOD;
using System;

namespace SNEngine.Audio.Internal;

/// <summary>
/// Reusable utility for linear volume fades on a specific FMOD Channel.
/// Handles the math, timing, and completion behavior (including optional stop).
/// 
/// Designed to be used by BgmPlayer, and potentially reusable for other
/// fading needs (e.g. ducking, voice fades, etc.).
/// </summary>
internal struct LinearChannelFade
{
    private float _startVol;
    private float _endVol;
    private float _duration;
    private float _timer;
    private bool _active;
    private bool _stopAfter;

    /// <summary>
    /// Whether a fade is currently in progress.
    /// </summary>
    public readonly bool IsActive => _active;

    /// <summary>
    /// If true, the channel should be stopped when this fade completes.
    /// </summary>
    public readonly bool StopAfterOnComplete => _stopAfter;

    /// <summary>
    /// Starts a new linear fade.
    /// </summary>
    /// <param name="fromVol">Starting volume (will be clamped 0..1)</param>
    /// <param name="toVol">Target volume (will be clamped 0..1)</param>
    /// <param name="duration">Duration in seconds. Values &lt;= 0 complete immediately.</param>
    /// <param name="stopAfter">If true, caller should stop the channel when fade completes.</param>
    public void Start(float fromVol, float toVol, float duration, bool stopAfter)
    {
        _startVol = Math.Clamp(fromVol, 0f, 1f);
        _endVol = Math.Clamp(toVol, 0f, 1f);
        _duration = Math.Max(0f, duration);
        _timer = 0f;
        _active = _duration > 0f;
        _stopAfter = stopAfter;

        // If zero duration, we consider it "started" but will complete on first Update.
        if (_duration <= 0f)
        {
            _active = true; // will finish immediately in Update
        }
    }

    /// <summary>
    /// Advances the fade by deltaTime and returns the current interpolated volume.
    /// </summary>
    /// <param name="deltaTime">Time since last update (in seconds).</param>
    /// <param name="completed">Set to true on the frame the fade reaches its end.</param>
    /// <returns>The volume that should be applied to the Channel right now.</returns>
    public float Update(float deltaTime, out bool completed)
    {
        completed = false;

        if (!_active)
            return _endVol;

        _timer += deltaTime;

        float t = _duration > 0f
            ? Math.Clamp(_timer / _duration, 0f, 1f)
            : 1f;

        float currentVol = _startVol + (_endVol - _startVol) * t;

        if (t >= 1f)
        {
            _active = false;
            completed = true;
            currentVol = _endVol; // ensure exact end value
        }

        return currentVol;
    }

    /// <summary>
    /// Cancels any active fade. Does not stop the channel.
    /// </summary>
    public void Cancel()
    {
        _active = false;
    }

    /// <summary>
    /// Convenience: applies the current fade value directly to the given channel (if active).
    /// Returns whether the fade just completed this frame.
    /// </summary>
    public bool UpdateAndApply(Channel channel, float deltaTime)
    {
        if (!channel.hasHandle() || !_active)
            return false;

        bool completed;
        float vol = Update(deltaTime, out completed);

        channel.setVolume(vol);

        if (completed && _stopAfter)
        {
            channel.stop();
        }

        return completed;
    }
}
