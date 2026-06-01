using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SNEngine.Core.Assets;
using SNEngine.Core.Engine;
using SNEngine.Core.Input;
using SNEngine.Core.Rendering;
using MouseButton = SNEngine.Core.Input.MouseButton;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading.Tasks;
using TrippyGL;

namespace SNEngine.Core.Engine;

/// <summary>
/// Главный хост движка с поддержкой обычного режима и Shared Memory превью.
/// </summary>
public class SNEngineHost : IDisposable, IFrameDataProvider
{
    private IWindow? _window;
    private GL? _gl;

    // Shared Memory для превью
    private SharedFramePublisher? _sharedFramePublisher;
    private readonly bool _useSharedMemory;
    private readonly int _previewWidth;
    private readonly int _previewHeight;

    // Reusable buffer for preview readback (allocated only in preview mode)
    private byte[]? _previewPixelBuffer;

    public AssetManager AssetManager { get; private set; } = null!;
    public Renderer Renderer { get; private set; } = null!;
    public SceneManager SceneManager { get; private set; } = null!;
    public FileManager FileManager { get; private set; } = null!;

    /// <summary>
    /// The TrippyGL GraphicsDevice. Primary graphics object after migration.
    /// </summary>
    public GraphicsDevice? GraphicsDevice { get; private set; }

    /// <summary>
    /// UI overlay (e.g. Ultralight). Rendered on top of the main game scene.
    /// Set from the outside (usually from SNEngine.Runtime or application entry point).
    /// 
    /// Note: This is the legacy single-overlay path. New code should prefer using <see cref="Ui"/>.
    /// </summary>
    public IUiOverlay? UiOverlay { get; set; }

    /// <summary>
    /// New recommended UI system that supports multiple independent UI elements
    /// (HTML panels, HUD, dialogs, etc.) with proper z-ordering.
    /// </summary>
    public UI.UiManager Ui { get; set; } = new UI.UiManager();

    /// <summary>
    /// Вызывается при изменении размера окна.
    /// </summary>
    public event Action<int, int>? Resized;

    /// <summary>
    /// Fired when the AssetManager has been created and is ready to receive packages.
    /// UI systems should subscribe to this to wire SnpkFileSystem early.
    /// </summary>
    public event Action<AssetManager>? AssetManagerInitialized;

    /// <summary>
    /// Render settings used by the engine. Can be customized before or after initialization.
    /// </summary>
    public RenderSettings RenderSettings { get; private set; } = new RenderSettings();

    public event Action? OnInitialized;

    /// <summary>
    /// Optional JavaScript bridge (e.g. Ultralight).
    /// Set this from SNEngine.API or your application after creating the host
    /// if you want JavaScript code to be able to call C# APIs.
    /// </summary>
    public Core.JS.IJSBridge? JavaScriptBridge { get; set; }

    private bool _isDisposing = false;
    private bool _disposed = false;

    private InternalGraphicsContext? _graphicsContext;

    // Lightweight frame profiler (only at Silk.NET host level).
    // Shows exactly which part of the frame (scene vs UI) is eating time.
    private readonly FrameProfiler _profiler = new();

    /// <summary>
    /// Графический API, который был использован при создании окна.
    /// Полезно для отладки и для условной логики (GLES vs Desktop GL).
    /// </summary>
    public GraphicsAPI GraphicsApi { get; }

    public double NativeFps => _profiler.NativeFps;

    /// <summary>
    /// Основной конструктор
    /// </summary>
    /// <param name="useSharedMemory">Включает режим превью через Shared Memory</param>
    /// <param name="graphicsApi">
    /// Позволяет явно задать графический API и версию контекста.
    /// По умолчанию используется OpenGL 4.6 Core (desktop).
    /// Для мобильных платформ обычно передают OpenGL ES (например, 3.2 или 3.0).
    /// </param>
    public SNEngineHost(string title = "SNEngine Test Window",
                        int width = 1280,
                        int height = 720,
                        bool useSharedMemory = false,
                        GraphicsAPI? graphicsApi = null,
                        RenderSettings? renderSettings = null)
    {
        _useSharedMemory = useSharedMemory;
        _previewWidth = width;
        _previewHeight = height;

        if (renderSettings != null)
            RenderSettings = renderSettings;

        var options = WindowOptions.Default;
        options.Title = title;
        options.Size = new Vector2D<int>(width, height);

        // Унифицированный выбор графического API.
        // Если не передан явно — используем разумный desktop GL по умолчанию.
        // Это позволяет использовать один и тот же хост для Windows/Linux/macOS и для мобильных (GLES).
        GraphicsApi = graphicsApi ?? new GraphicsAPI(
            ContextAPI.OpenGL,
            ContextProfile.Core,
            ContextFlags.Default,
            new APIVersion(4, 6));

        options.API = GraphicsApi;
        options.VSync = true;

        // === SILK.NET TIMING FIX (VSync + FPS issue) ===
        // Без явного указания FramesPerSecond=0 при VSync=true унаследованные из WindowOptions.Default
        // значения UpdatesPerSecond/FramesPerSecond (обычно 60) конфликтуют с драйверным vsync (SwapBuffers блокируется на vblank).
        // Результат: нестабильный/заниженный FPS (в т.ч. ~36 вместо 60), который видит requestAnimationFrame внутри Ultralight.
        // Фикс только на уровне Silk.NET window options (Ultralight не трогаем).
        //   FramesPerSecond = 0  → рендер driven чисто vsync'ом (никакого софт. таймера)
        //   UpdatesPerSecond = 60 → логика/симуляция на стабильных 60 Hz (deltaTime ~1/60)
        options.FramesPerSecond = 0;
        options.UpdatesPerSecond = 60;


        // Для превью можно скрыть окно
        if (useSharedMemory)
        {
            options.IsVisible = false;
            Console.WriteLine("[Preview] Shared Memory mode enabled. Window will be hidden.");
        }

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Update += OnUpdateFrame;
        _window.Render += OnRenderFrame;
        _window.Closing += OnClosing;
        _window.Resize += OnResize;
    }

    private void OnLoad()
    {
        _gl = GL.GetApi(_window);

        // === INPUT SYSTEM ===
        var inputProvider = new Input.SilkInputProvider(_window);
        Input.Input.Initialize(inputProvider);
        Debug.Log("[SNEngineHost] Input system initialized (Silk.NET).");

        if (!_useSharedMemory)
            CenterWindow();

        // === TRIPPYGL MIGRATION ===
        // Create GraphicsDevice first — textures and renderer need it
        GraphicsDevice = new GraphicsDevice(_gl);
        Console.WriteLine("[SNEngineHost] TrippyGL GraphicsDevice created.");

        // Apply render settings to the device early
        GraphicsDevice.ClearColor = RenderSettings.ClearColor;
        GraphicsDevice.BlendState = RenderSettings.BlendState;

        // Asset managers now use GraphicsDevice (with legacy GL fallback inside)
        AssetManager = new AssetManager(GraphicsDevice);
        FileManager = new FileManager(GraphicsDevice);

        // Notify that AssetManager is ready. The API layer (SNEngine) will use this
        // to wire it to any UI overlays/elements so that SnpkFileSystem gets applied.
        AssetManagerInitialized?.Invoke(AssetManager);

        Renderer = new Renderer();
        // Preferred path: pass existing GraphicsDevice + our settings
        Renderer.Initialize(GraphicsDevice, RenderSettings);

        // Initial viewport + projection
        Renderer.SetViewport(_previewWidth, _previewHeight);

        SceneManager = new SceneManager();

        Debug.Initialize();

        if (_useSharedMemory)
        {
            _sharedFramePublisher = new SharedFramePublisher();
            _sharedFramePublisher.Initialize(_previewWidth, _previewHeight);

            // Rent a reusable buffer for ReadPixels (big win for preview performance)
            int bufferSize = _previewWidth * _previewHeight * 4;
            _previewPixelBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            Debug.Log("[Preview] Shared Memory Publisher initialized successfully.");
        }

        Debug.Log("SNEngineHost: All systems initialized successfully (TrippyGL path).");

        // Create graphics context for UI (using delegates to avoid tight coupling)
        _graphicsContext = new InternalGraphicsContext(
            () => GraphicsDevice,
            () => Renderer?.ViewportWidth ?? 0,
            () => Renderer?.ViewportHeight ?? 0
        );

        // Initialize new UI system (preferred)
        Ui?.Initialize(_graphicsContext);

        // Legacy single-overlay path (still supported for backward compatibility)
        UiOverlay?.Initialize(_graphicsContext);

        OnInitialized?.Invoke();
    }

    private void CenterWindow()
    {
        if (_window?.Monitor == null) return;

        var monitor = _window.Monitor;
        var bounds = monitor.Bounds;

        int centerX = bounds.Origin.X + (bounds.Size.X - _window.Size.X) / 2;
        int centerY = bounds.Origin.Y + (bounds.Size.Y - _window.Size.Y) / 2;

        _window.Position = new Vector2D<int>(centerX, centerY);
        Debug.Log($"Window centered at ({centerX}, {centerY})");
    }

    private void OnUpdateFrame(double deltaTime)
    {
        if (_isDisposing) return;

        // Update global Time system first (must happen before any system uses delta)
        Engine.Time.Update(deltaTime);

        // Update input state (edge detection for buttons/keys)
        Input.Input.Update();

        // === Forward mouse input to Ultralight views ===
        ProcessInputToUltralightViews();

        _profiler.BeginUpdate();

        _profiler.Time("Update/Scene", () =>
        {
            SceneManager?.Update(deltaTime);
        });

        _profiler.Time("Update/UI", () =>
        {
            // Update UI elements (logic, JS, animations, etc.)
            Ui?.Update(deltaTime);
        });

        // Drive Core-level systems that produce runtime data for UI
        _profiler.Time("Update/DialogueSystem", () =>
        {
            // DialogueSystem now prefers Engine.Time.SmoothDeltaTime internally
            DialogueSystem.Update();
        });

        // Push runtime data (FPS + current dialogue with typewriter progress, etc.)
        // from Core into all active UI elements. The elements themselves no longer
        // decide what data to collect or push — they only receive.
        _profiler.Time("Update/RuntimeDataPush", () =>
        {
            PushRuntimeDataToUiElements();
        });

        // Process any pending calls from JavaScript (e.g. from Ultralight)
        _profiler.Time("Update/JSBridge", () =>
        {
            JavaScriptBridge?.ProcessPendingCalls();
        });

        _profiler.EndUpdate();
    }

    // TODO: Implement proper input routing to Ultralight views
    // private void ProcessInputToUltralightViews() { ... }

    /// <summary>
    /// Collects current runtime data from Core systems (FPS from profiler, dialogue from DialogueSystem)
    /// and pushes the snapshot to every active UI element.
    /// 
    /// This is the central place where "what runtime data UI sees" is decided.
    /// Individual UI elements only receive — they do not hardcode knowledge of FPS or dialogue.
    /// </summary>
    private void PushRuntimeDataToUiElements()
    {
        if (Ui == null || Ui.Elements.Count == 0)
            return;

        var snapshot = new RuntimeSnapshot
        {
            Fps = this.NativeFps,
            Dialogue = DialogueSystem.GetSnapshot()
        };

        foreach (var element in Ui.Elements)
        {
            if (!element.Visible) continue;

            try
            {
                element.ReceiveRuntimeData(in snapshot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SNEngineHost] Error pushing runtime data to UI element: {ex.Message}");
            }
        }
    }

    // Simple previous state tracking for mouse button edge detection
    private bool _prevLeftMouse;
    private bool _prevRightMouse;
    private bool _prevMiddleMouse;

    private void ProcessInputToUltralightViews()
    {
        if (Ui == null) return;

        var mousePos = Input.Input.MousePosition;

        // Forward mouse movement (important for hover states, cursor changes, etc. in HTML)
        Ui.ProcessMouseMove(mousePos.X, mousePos.Y);

        // Forward button events
        bool left = Input.Input.GetMouseButton(MouseButton.Left);
        if (left && !_prevLeftMouse)
            Ui.ProcessMouseButton(MouseButton.Left, true, mousePos.X, mousePos.Y);
        else if (!left && _prevLeftMouse)
            Ui.ProcessMouseButton(MouseButton.Left, false, mousePos.X, mousePos.Y);

        bool right = Input.Input.GetMouseButton(MouseButton.Right);
        if (right && !_prevRightMouse)
            Ui.ProcessMouseButton(MouseButton.Right, true, mousePos.X, mousePos.Y);
        else if (!right && _prevRightMouse)
            Ui.ProcessMouseButton(MouseButton.Right, false, mousePos.X, mousePos.Y);

        bool middle = Input.Input.GetMouseButton(MouseButton.Middle);
        if (middle && !_prevMiddleMouse)
            Ui.ProcessMouseButton(MouseButton.Middle, true, mousePos.X, mousePos.Y);
        else if (!middle && _prevMiddleMouse)
            Ui.ProcessMouseButton(MouseButton.Middle, false, mousePos.X, mousePos.Y);

        _prevLeftMouse = left;
        _prevRightMouse = right;
        _prevMiddleMouse = middle;
    }

    private void OnRenderFrame(double deltaTime)
    {
        if (Renderer == null || _isDisposing) return;

        _profiler.BeginRender();

        _profiler.Time("Render/Scene", () =>
        {
            Renderer.Clear();
            Renderer.Begin();
            SceneManager?.Render(Renderer);
            Renderer.End();
        });

        // === NEW UI SYSTEM (multiple elements with z-ordering) ===
        // This is usually the heaviest part: it calls the Ultralight PreRenderHook
        // (Update + Render for all views) + uploads surfaces + draws them.
        // We time the whole block without touching any Ultralight code.
        _profiler.Time("Render/UI", () =>
        {
            if (Ui != null && _graphicsContext != null)
            {
                Ui.Render(_graphicsContext);
            }

            // === LEGACY SINGLE UI OVERLAY (for backward compatibility) ===
            if (UiOverlay != null && _graphicsContext != null)
            {
                UiOverlay.Render(_graphicsContext);
            }
        });

        // === SHARED MEMORY ПРЕВЬЮ ===
        if (_useSharedMemory && _sharedFramePublisher != null && GraphicsDevice != null)
        {
            PublishPreviewFrame();
        }

        _profiler.EndRenderAndMaybeLog();
    }

    private unsafe void PublishPreviewFrame()
    {
        if (!_useSharedMemory || _sharedFramePublisher == null || _previewPixelBuffer == null)
            return;

        try
        {
            int w = _previewWidth;
            int h = _previewHeight;
            int neededSize = w * h * 4;

            // Use pre-rented buffer (no allocation per frame)
            byte[] pixels = _previewPixelBuffer;

            var gl = GraphicsDevice?.GL ?? _gl;
            if (gl == null) return;

            fixed (byte* ptr = pixels)
            {
                gl.ReadPixels(0, 0, (uint)w, (uint)h, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }

            _sharedFramePublisher.PublishFrame(w, h, pixels.AsSpan(0, neededSize));
        }
        catch { }
    }

    private void OnClosing()
    {
        if (_isDisposing || _window == null) return;

        _isDisposing = true;

        // Отключаем все события
        _window.Load -= OnLoad;
        _window.Update -= OnUpdateFrame;
        _window.Render -= OnRenderFrame;
        _window.Closing -= OnClosing;
        _window.Resize -= OnResize;

        Task.Run(SafeDispose);
    }

    private void OnResize(Vector2D<int> newSize)
    {
        if (Renderer == null) return;

        // Many window systems report (0,0) when the window is minimized / iconified.
        // We must not propagate 0-size down to renderers, textures, or Ultralight views.
        if (newSize.X <= 0 || newSize.Y <= 0)
            return;

        Renderer.SetViewport(newSize.X, newSize.Y);
        Debug.Log($"Window resized to {newSize.X}x{newSize.Y}");

        // Notify new UI system
        Ui?.Resize(newSize.X, newSize.Y);

        // Уведомляем подписчиков о ресайзе (в т.ч. SNEngine для активных LoadScreen)
        Resized?.Invoke(newSize.X, newSize.Y);

        // Legacy overlay
        UiOverlay?.Resize(newSize.X, newSize.Y);
    }

    private void SafeDispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_window == null) return;

        try
        {
            // New UI system (dispose before legacy overlay)
            if (Ui != null)
            {
                try
                {
                    Ui.Dispose();
                }
                catch (Exception ex)
                {
                    // Many "NoContext" / "Wrong thread" errors are normal here because
                    // SafeDispose often runs on a background thread via Task.Run.
                    if (ex.Message?.Contains("NoContext", StringComparison.OrdinalIgnoreCase) == true ||
                        ex.Message?.Contains("Wrong thread", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        Debug.Log($"[Shutdown] UiManager disposed after context/thread change (normal).");
                    }
                    else
                    {
                        Debug.LogError($"UiManager dispose error: {ex.Message}");
                    }
                }
                Ui = null;
            }

            // Legacy UI Overlay (dispose first before heavy graphics resources)
            if (UiOverlay != null)
            {
                try
                {
                    UiOverlay.Dispose();
                }
                catch (Exception ex)
                {
                    // "NoContext" errors during shutdown are normal and expected.
                    // The OpenGL context is already destroyed by the time we reach Dispose.
                    if (ex.Message?.Contains("NoContext", StringComparison.OrdinalIgnoreCase) == true ||
                        ex.Message?.Contains("current OpenGL", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        Debug.Log($"[Shutdown] UiOverlay disposed after context was destroyed (normal).");
                    }
                    else
                    {
                        Debug.LogError($"UiOverlay dispose error: {ex.Message}");
                    }
                }
                UiOverlay = null;
            }

            // Очищаем Shared Memory
            if (_sharedFramePublisher != null)
            {
                try { _sharedFramePublisher.Dispose(); }
                catch { }
                _sharedFramePublisher = null;
            }

            // Return rented preview buffer to ArrayPool
            if (_previewPixelBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_previewPixelBuffer);
                _previewPixelBuffer = null;
            }

            // Ресурсы рендера (TrippyGL) — важно соблюдать порядок
            if (Renderer != null)
            {
                try { Renderer.Dispose(); }
                catch { }
                Renderer = null!;
            }

            if (AssetManager != null)
            {
                try { AssetManager.Dispose(); }
                catch { }
                AssetManager = null!;
            }

            if (FileManager != null)
            {
                // FileManager currently doesn't own heavy resources in the main path
            }

            if (GraphicsDevice != null)
            {
                try { GraphicsDevice.Dispose(); }
                catch { }
                GraphicsDevice = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Dispose resources error: {ex.Message}");
        }

        try
        {
            _window?.Dispose();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Window dispose error: {ex.Message}");
        }
        finally
        {
            _window = null;
            _gl = null;
            SceneManager = null!;
            FileManager = null!;
            _graphicsContext = null;
        }
    }

    public void Run() => _window?.Run();

    public void Dispose()
    {
        SafeDispose();
        GC.SuppressFinalize(this);
    }

    // ============================================================
    // Lightweight frame profiler (Silk.NET host level only)
    // Use this to answer "what exactly is eating the frame time?"
    // It never touches Ultralight code — only measures call sites.
    // ============================================================
    private sealed class FrameProfiler : IFrameDataProvider
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
}