using System.Diagnostics;

namespace SNEngine.Core.Engine;

    // ============================================================
    // Lightweight frame profiler (Silk.NET host level only)
    // Use this to answer "what exactly is eating the frame time?"
    // It never touches Ultralight code — only measures call sites.
    // ============================================================
    public  class FrameProfiler : IFrameDataProvider
    {
        private readonly Stopwatch _frameSw = new();
        private readonly Stopwatch _sectionSw = new();

        private readonly Stopwatch _logIntervalSw = new();
        private int _framesSinceLog;

        // Accumulators (in milliseconds)
        private double _sumInterFrameMs;
        private double _sumUpdateScene;
        private double _sumUpdateUi;
        private double _sumRenderScene;
        private double _sumRenderUi;

        private double _currentUpdateScene;
        private double _currentUpdateUi;

        public double NativeFps { get; private set; }

        public FrameProfiler()
        {
            _frameSw.Start();
            _logIntervalSw.Start();
        }

        private bool _firstFrame = true;

        /// <summary>
        /// Call at the very start of OnRenderFrame (before any work).
        /// </summary>
        public void BeginRender()
        {
            _sectionSw.Restart();

            // Inter-frame time (real time between render callbacks, includes vsync wait + previous work)
            double interFrameMs = _frameSw.Elapsed.TotalMilliseconds;
            if (!_firstFrame)
                _sumInterFrameMs += interFrameMs;
            _frameSw.Restart();
            _firstFrame = false;
        }

        public void Time(string section, Action action)
        {
            _sectionSw.Restart();
            action();
            double ms = _sectionSw.Elapsed.TotalMilliseconds;

            switch (section)
            {
                case "Render/Scene":
                    _sumRenderScene += ms;
                    break;
                case "Render/UI":
                    _sumRenderUi += ms;
                    break;
                case "Update/Scene":
                    _currentUpdateScene = ms;
                    break;
                case "Update/UI":
                    _currentUpdateUi = ms;
                    break;
            }
        }

        public void BeginUpdate()
        {
            // nothing special yet
        }

        public void EndUpdate()
        {
            _sumUpdateScene += _currentUpdateScene;
            _sumUpdateUi += _currentUpdateUi;
            _currentUpdateScene = 0;
            _currentUpdateUi = 0;
        }

        /// <summary>
        /// Call at the very end of OnRenderFrame. Will log averages once per second.
        /// </summary>
        public void EndRenderAndMaybeLog()
        {
            _framesSinceLog++;

            if (_logIntervalSw.Elapsed.TotalMilliseconds >= 1000.0)
            {
                double frames = Math.Max(1, _framesSinceLog);
                double avgFrame = _sumInterFrameMs / frames;
                double avgUpdateScene = _sumUpdateScene / frames;
                double avgUpdateUi = _sumUpdateUi / frames;
                double avgRenderScene = _sumRenderScene / frames;
                double avgRenderUi = _sumRenderUi / frames;

                double totalAccounted = avgUpdateScene + avgUpdateUi + avgRenderScene + avgRenderUi;
                double fpsFromInterFrame = avgFrame > 0 ? 1000.0 / avgFrame : 0;
                NativeFps = fpsFromInterFrame;
                Debug.Log(
                    $"[FrameProfiler] FPS~{fpsFromInterFrame:F1} | " +
                    $"Frame: {avgFrame:F2}ms | " +
                    $"Update(Scene/UI): {avgUpdateScene:F2}+{avgUpdateUi:F2}ms | " +
                    $"Render(Scene/UI): {avgRenderScene:F2}+{avgRenderUi:F2}ms | " +
                    $"Accounted: {totalAccounted:F2}ms");

                // Reset accumulators
                _sumInterFrameMs = 0;
                _sumUpdateScene = 0;
                _sumUpdateUi = 0;
                _sumRenderScene = 0;
                _sumRenderUi = 0;
                _framesSinceLog = 0;
                _logIntervalSw.Restart();
            }
        }
    }