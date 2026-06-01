using System.Collections.Generic;

namespace SNEngine.Core.Engine;

/// <summary>
/// Centralized time management, similar to Unity's Time class.
/// 
/// This is the single source of truth for deltaTime, total time, time scale, etc.
/// All systems (DialogueSystem, animations, UI, etc.) should eventually read time data from here
/// instead of receiving raw deltaTime from the game loop.
/// 
/// This helps eliminate frame-time jitter issues (especially noticeable in typewriter effects).
/// </summary>
public static class Time
{
    private static float _deltaTime;
    private static float _unscaledDeltaTime;
    private static float _smoothDeltaTime;
    private static float _time;
    private static float _unscaledTime;
    private static float _timeScale = 1f;

    // For smooth delta time calculation
    private const int SmoothDeltaSamples = 10;
    private static readonly Queue<float> _deltaHistory = new Queue<float>(SmoothDeltaSamples);
    private static float _deltaSum;

    /// <summary>
    /// The time in seconds it took to complete the last frame.
    /// Affected by <see cref="TimeScale"/>.
    /// </summary>
    public static float DeltaTime => _deltaTime;

    /// <summary>
    /// The time in seconds it took to complete the last frame, unaffected by <see cref="TimeScale"/>.
    /// </summary>
    public static float UnscaledDeltaTime => _unscaledDeltaTime;

    /// <summary>
    /// A smoothed version of <see cref="DeltaTime"/>. 
    /// Uses a moving average over the last few frames to reduce jitter.
    /// Recommended for animation and typewriter systems.
    /// </summary>
    public static float SmoothDeltaTime => _smoothDeltaTime;

    /// <summary>
    /// The total number of seconds that have passed since the game started (affected by TimeScale).
    /// Unity equivalent: Time.time
    /// </summary>
    public static float ElapsedTime => _time;

    /// <summary>
    /// The total number of seconds that have passed since the game started (ignores TimeScale).
    /// </summary>
    public static float UnscaledElapsedTime => _unscaledTime;

    /// <summary>
    /// The scale at which time passes. 
    /// 1.0 = normal speed, 0.5 = half speed, 0 = paused.
    /// </summary>
    public static float TimeScale
    {
        get => _timeScale;
        set => _timeScale = value < 0 ? 0 : value;
    }

    /// <summary>
    /// Should be called once per frame by the engine host (SNEngineHost).
    /// </summary>
    internal static void Update(double rawDeltaTime)
    {
        _unscaledDeltaTime = (float)rawDeltaTime;

        // Apply timescale
        _deltaTime = _unscaledDeltaTime * _timeScale;

        // Update total times
        _unscaledTime += _unscaledDeltaTime;
        _time += _deltaTime;

        // Update smoothed delta
        UpdateSmoothDelta(_deltaTime);
    }

    private static void UpdateSmoothDelta(float newDelta)
    {
        // Keep a rolling window of recent delta times
        if (_deltaHistory.Count >= SmoothDeltaSamples)
        {
            float oldest = _deltaHistory.Dequeue();
            _deltaSum -= oldest;
        }

        _deltaHistory.Enqueue(newDelta);
        _deltaSum += newDelta;

        // Calculate average
        if (_deltaHistory.Count > 0)
        {
            _smoothDeltaTime = _deltaSum / _deltaHistory.Count;
        }
        else
        {
            _smoothDeltaTime = newDelta;
        }

        // Clamp to reasonable bounds (avoid division by zero or huge spikes in dependent systems)
        if (_smoothDeltaTime < 0.0001f)
            _smoothDeltaTime = 0.0001f;
    }

    /// <summary>
    /// Resets all time values. Useful for restarting the game or entering a new scene.
    /// </summary>
    public static void Reset()
    {
        _time = 0;
        _unscaledTime = 0;
        _deltaTime = 0;
        _unscaledDeltaTime = 0;
        _smoothDeltaTime = 0;
        _timeScale = 1f;

        _deltaHistory.Clear();
        _deltaSum = 0;
    }
}
