using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SNEngine.Core.Assets;
using SNEngine.Core.Engine;
using SNEngine.Core.Rendering;
using System;
using System.Buffers;
using System.Threading.Tasks;
using TrippyGL;

namespace SNEngine.Core.Engine;

/// <summary>
/// Главный хост движка с поддержкой обычного режима и Shared Memory превью.
/// </summary>
public class SNEngineHost : IDisposable
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

    private bool _isDisposing = false;
    private bool _disposed = false;

    private InternalGraphicsContext? _graphicsContext;

    /// <summary>
    /// Графический API, который был использован при создании окна.
    /// Полезно для отладки и для условной логики (GLES vs Desktop GL).
    /// </summary>
    public GraphicsAPI GraphicsApi { get; }

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

        SceneManager?.Update(deltaTime);

        // Update UI elements (logic, JS, animations, etc.)
        Ui?.Update(deltaTime);
    }

    private void OnRenderFrame(double deltaTime)
    {
        if (Renderer == null || _isDisposing) return;

        Renderer.Clear();
        Renderer.Begin();
        SceneManager?.Render(Renderer);
        Renderer.End();

        // === NEW UI SYSTEM (multiple elements with z-ordering) ===
        if (Ui != null && _graphicsContext != null)
        {
            Ui.Render(_graphicsContext);
        }

        // === LEGACY SINGLE UI OVERLAY (for backward compatibility) ===
        if (UiOverlay != null && _graphicsContext != null)
        {
            UiOverlay.Render(_graphicsContext);
        }

        // === SHARED MEMORY ПРЕВЬЮ ===
        if (_useSharedMemory && _sharedFramePublisher != null && GraphicsDevice != null)
        {
            PublishPreviewFrame();
        }
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
                    Debug.LogError($"UiManager dispose error: {ex.Message}");
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
}