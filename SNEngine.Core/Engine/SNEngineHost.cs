using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;
using System;
using System.Threading.Tasks;

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

    public AssetManager AssetManager { get; private set; } = null!;
    public Renderer Renderer { get; private set; } = null!;
    public SceneManager SceneManager { get; private set; } = null!;
    public FileManager FileManager { get; private set; } = null!;

    public event Action? OnInitialized;

    private bool _isDisposing = false;

    /// <summary>
    /// Основной конструктор
    /// </summary>
    /// <param name="useSharedMemory">Включает режим превью через Shared Memory</param>
    public SNEngineHost(string title = "SNEngine Test Window",
                        int width = 1280,
                        int height = 720,
                        bool useSharedMemory = false)
    {
        _useSharedMemory = useSharedMemory;
        _previewWidth = width;
        _previewHeight = height;

        var options = WindowOptions.Default;
        options.Title = title;
        options.Size = new Vector2D<int>(width, height);
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(4, 6));
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

        AssetManager = new AssetManager(_gl);
        FileManager = new FileManager(_gl);
        Renderer = new Renderer();
        Renderer.Initialize(_gl);
        SceneManager = new SceneManager();

        Debug.Initialize();

        if (_useSharedMemory)
        {
            _sharedFramePublisher = new SharedFramePublisher();
            _sharedFramePublisher.Initialize(_previewWidth, _previewHeight);
            Debug.Log("[Preview] Shared Memory Publisher initialized successfully.");
        }

        Debug.Log("SNEngineHost: All systems initialized successfully.");
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
    }

    private void OnRenderFrame(double deltaTime)
    {
        if (Renderer == null || _isDisposing) return;

        Renderer.Clear();
        Renderer.Begin();
        SceneManager?.Render(Renderer);
        Renderer.End();

        // === SHARED MEMORY ПРЕВЬЮ ===
        if (_useSharedMemory && _sharedFramePublisher != null && _gl != null)
        {
            PublishPreviewFrame();
        }
    }

    private unsafe void PublishPreviewFrame()
    {
        try
        {
            int w = _previewWidth;
            int h = _previewHeight;
            byte[] pixels = new byte[w * h * 4];   // можно сделать static/reusable

            fixed (byte* ptr = pixels)
            {
                _gl!.ReadPixels(0, 0, (uint)w, (uint)h, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }

            _sharedFramePublisher!.PublishFrame(w, h, pixels.AsSpan());
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
        if (_gl == null) return;

        _gl.Viewport(0, 0, (uint)newSize.X, (uint)newSize.Y);
        Debug.Log($"Window resized to {newSize.X}x{newSize.Y}");
    }

    private void SafeDispose()
    {
        if (_window == null) return;

        try
        {
            // Очищаем Shared Memory
            if (_sharedFramePublisher != null)
            {
                try { _sharedFramePublisher.Dispose(); }
                catch { }
                _sharedFramePublisher = null;
            }

            // Ресурсы рендера
            if (Renderer != null)
            {
                try { Renderer.Dispose(); }
                catch { }
            }

            if (AssetManager != null)
            {
                try { AssetManager.Dispose(); }
                catch { }
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
            Renderer = null!;
            AssetManager = null!;
            SceneManager = null!;
            FileManager = null!;
        }
    }

    public void Run() => _window?.Run();

    public void Dispose() => SafeDispose();
}