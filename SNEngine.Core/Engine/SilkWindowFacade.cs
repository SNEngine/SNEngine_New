using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;
using System.Diagnostics;

namespace SNEngine.Core.Engine;

/// <summary>
/// Facade responsible for Silk.NET window creation, configuration, 
/// and lifecycle management. Extracted to reduce complexity of SNEngineHost.
/// </summary>
public class SilkWindowFacade : IDisposable
{
    private IWindow? _window;
    private readonly string _title;
    private readonly int _width;
    private readonly int _height;
    private readonly bool _useSharedMemory;
    private readonly GraphicsAPI _graphicsApi;

    /// <summary>
    /// Fired when the window is loaded.
    /// </summary>
    public event Action? Load;

    /// <summary>
    /// Fired on update frame.
    /// </summary>
    public event Action<double>? Update;

    /// <summary>
    /// Fired on render frame.
    /// </summary>
    public event Action<double>? Render;

    /// <summary>
    /// Fired when the window is about to close.
    /// </summary>
    public event Action? Closing;

    /// <summary>
    /// Fired when the window is resized.
    /// </summary>
    public event Action<Vector2D<int>>? Resize;

    public IWindow? Window => _window;

    public SilkWindowFacade(
        string title = "SNEngine Test Window",
        int width = 1280,
        int height = 720,
        bool useSharedMemory = false,
        GraphicsAPI? graphicsApi = null)
    {
        _title = title;
        _width = width;
        _height = height;
        _useSharedMemory = useSharedMemory;
        _graphicsApi = graphicsApi ?? new GraphicsAPI(
            ContextAPI.OpenGL,
            ContextProfile.Core,
            ContextFlags.Default,
            new APIVersion(4, 6));
    }

    /// <summary>
    /// Creates and configures the Silk.NET window with proper VSync settings.
    /// </summary>
    public void CreateWindow()
    {
        var options = WindowOptions.Default;
        options.Title = _title;
        options.Size = new Vector2D<int>(_width, _height);
        options.API = _graphicsApi;
        options.VSync = true;
        options.FramesPerSecond = 0;
        options.UpdatesPerSecond = 60;

        if (_useSharedMemory)
        {
            options.IsVisible = false;
            Console.WriteLine("[Preview] Shared Memory mode enabled. Window will be hidden.");
        }

        _window = Silk.NET.Windowing.Window.Create(options);

        _window.Load += OnLoadInternal;
        _window.Update += OnUpdateInternal;
        _window.Render += OnRenderInternal;
        _window.Closing += OnClosingInternal;
        _window.Resize += OnResizeInternal;

        Debug.Log($"[SilkWindowFacade] Window created: {_width}x{_height}");
    }

    private void OnLoadInternal() => Load?.Invoke();
    private void OnUpdateInternal(double delta) => Update?.Invoke(delta);
    private void OnRenderInternal(double delta) => Render?.Invoke(delta);
    private void OnClosingInternal() => Closing?.Invoke();
    private void OnResizeInternal(Vector2D<int> size) => Resize?.Invoke(size);

    /// <summary>
    /// Centers the window on the primary monitor.
    /// </summary>
    public void CenterWindow()
    {
        if (_window?.Monitor == null) return;

        var monitor = _window.Monitor;
        var bounds = monitor.Bounds;

        int centerX = bounds.Origin.X + (bounds.Size.X - _window.Size.X) / 2;
        int centerY = bounds.Origin.Y + (bounds.Size.Y - _window.Size.Y) / 2;

        _window.Position = new Vector2D<int>(centerX, centerY);
        Debug.Log($"Window centered at ({centerX}, {centerY})");
    }

    /// <summary>
    /// Starts the main window message loop.
    /// </summary>
    public void Run() => _window?.Run();

    /// <summary>
    /// Closes the window.
    /// </summary>
    public void Close() => _window?.Close();

    public void Dispose()
    {
        if (_window == null) return;

        try
        {
            _window.Load -= OnLoadInternal;
            _window.Update -= OnUpdateInternal;
            _window.Render -= OnRenderInternal;
            _window.Closing -= OnClosingInternal;
            _window.Resize -= OnResizeInternal;

            _window.Dispose();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error disposing SilkWindowFacade: {ex.Message}");
        }
        finally
        {
            _window = null;
        }
    }
}