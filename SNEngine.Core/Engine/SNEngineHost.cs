using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;

namespace SNEngine.Core.Engine;

/// <summary>
/// Main engine host with safe cleanup, centered window and robust disposal.
/// </summary>
public class SNEngineHost : IDisposable
{
    private IWindow? _window;
    private GL? _gl;

    public AssetManager AssetManager { get; private set; } = null!;
    public Renderer Renderer { get; private set; } = null!;
    public SceneManager SceneManager { get; private set; } = null!;
    public FileManager FileManager { get; private set; } = null!;

    public event Action? OnInitialized;

    private bool _isDisposing = false;

    public SNEngineHost(string title = "SNEngine Test Window", int width = 1280, int height = 720)
    {
        var options = WindowOptions.Default;
        options.Title = title;
        options.Size = new Silk.NET.Maths.Vector2D<int>(width, height);
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(4, 6));
        options.VSync = true;

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Update += OnUpdateFrame;
        _window.Render += OnRenderFrame;
        _window.Closing += OnClosing;
        _window.Resize += OnResize;   // ← добавь эту строку
    }

    private void OnLoad()
    {
        _gl = GL.GetApi(_window);

        CenterWindow();

        AssetManager = new AssetManager(_gl);
        FileManager = new FileManager(_gl);
        Renderer = new Renderer();
        Renderer.Initialize(_gl);
        SceneManager = new SceneManager();

        Debug.Initialize();

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

        _window.Position = new Silk.NET.Maths.Vector2D<int>(centerX, centerY);
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
        _window.Resize -= OnResize;   // ← добавь эту строку


        // Максимально отложенный Dispose
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
            // Сначала отключаем рендерер без вызова OpenGL
            if (Renderer != null)
            {
                try { Renderer.Dispose(); }
                catch { /* Игнорируем, т.к. контекст может быть уже потерян */ }
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
        }
    }


    public void Run() => _window?.Run();

    public void Dispose() => SafeDispose();
}