using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;
using SNEngine.Core.Scenes;
using System;

namespace SNEngine.Core.Engine;

public class SNEngineHost : IDisposable
{
    private IWindow? _window;
    private GL? _gl;

    public AssetManager AssetManager { get; private set; } = null!;
    public Renderer Renderer { get; private set; } = null!;
    public SceneManager SceneManager { get; private set; } = null!;

    public event Action? OnInitialized;

    public SNEngineHost(string title = "SNEngine Test Window", int width = 1280, int height = 720)
    {
        var options = WindowOptions.Default;
        options.Title = title;
        options.Size = new Silk.NET.Maths.Vector2D<int>(width, height);
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(4, 6));
        options.VSync = true;
        options.WindowState = WindowState.Normal;

        _window = Window.Create(options);

        _window.Load += () =>
        {
            var monitor = _window.Monitor; 
            if (monitor != null)
            {
                var centerX = monitor.Bounds.Origin.X + (monitor.Bounds.Size.X - width) / 2;
                var centerY = monitor.Bounds.Origin.Y + (monitor.Bounds.Size.Y - height) / 2;

                _window.Position = new Silk.NET.Maths.Vector2D<int>(centerX, centerY);
            }
        };

        _window.Load += OnLoad;
        _window.Update += OnUpdateFrame;
        _window.Render += OnRenderFrame;
        _window.Closing += OnClosing;
    }

    private void OnLoad()
    {
        _gl = GL.GetApi(_window);

        AssetManager = new AssetManager(_gl);
        Renderer = new Renderer();           // ← здесь передаём GL
        SceneManager = new SceneManager();

        Console.WriteLine("✅ SNEngineHost: All systems initialized with OpenGL.");
        Renderer.Initialize(_gl);              // ← инициализируем Renderer с GL

        OnInitialized?.Invoke();
    }

    private void OnUpdateFrame(double deltaTime) => SceneManager?.Update(deltaTime);

    private void OnRenderFrame(double deltaTime)
    {
        Renderer.Clear();
        Renderer.Begin();
        SceneManager.Render(Renderer);
        Renderer.End();
    }

    private void OnClosing() => Dispose();

    public void Run() => _window?.Run();

    public void Dispose()
    {
        Renderer?.Dispose();
        AssetManager?.Dispose();
        _window?.Dispose();
    }
}