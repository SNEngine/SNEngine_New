using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SNEngine.Core.Assets;
using SNEngine.Core.Input;
using SNEngine.Core.Rendering;
using System;
using System.Threading.Tasks;
using TrippyGL;

namespace SNEngine.Core.Engine;

/// <summary>
/// Main engine host with support for normal window mode and Shared Memory preview.
/// </summary>
public partial class SNEngineHost : IDisposable, IFrameDataProvider
{
    private readonly SilkWindowFacade _windowFacade;
    private readonly PreviewSystem _previewSystem;
    private readonly GraphicsInitializer _graphicsInitializer;
    private readonly InputRouter _inputRouter;
    private readonly RuntimeDataPusher _runtimeDataPusher;

    public AssetManager AssetManager { get; private set; } = null!;
    public Renderer Renderer => _graphicsInitializer.Renderer!;
    public SceneManager SceneManager { get; private set; } = null!;
    public FileManager FileManager { get; private set; } = null!;

    public GraphicsDevice? GraphicsDevice => _graphicsInitializer.GraphicsDevice;

    public IUiOverlay? UiOverlay { get; set; }
    public UI.UiManager Ui { get; set; } = new UI.UiManager();

    public event Action<int, int>? Resized;
    public event Action<AssetManager>? AssetManagerInitialized;
    public event Action? OnInitialized;

    public Core.JS.IJSBridge? JavaScriptBridge { get; set; }

    public RenderSettings RenderSettings { get; private set; } = new RenderSettings();
    public GraphicsAPI GraphicsApi { get; }

    public double NativeFps => _profiler.NativeFps;

    private readonly FrameProfiler _profiler = new();
    private InternalGraphicsContext? _graphicsContext;

    private bool _isDisposing = false;
    private bool _disposed = false;

    public SNEngineHost(
        string title = "SNEngine Test Window",
        int width = 1280,
        int height = 720,
        bool useSharedMemory = false,
        GraphicsAPI? graphicsApi = null,
        RenderSettings? renderSettings = null)
    {
        if (renderSettings != null)
            RenderSettings = renderSettings;

        GraphicsApi = graphicsApi ?? new GraphicsAPI(
            ContextAPI.OpenGL,
            ContextProfile.Core,
            ContextFlags.Default,
            new APIVersion(4, 6));

        _windowFacade = new SilkWindowFacade(title, width, height, useSharedMemory, GraphicsApi);
        _previewSystem = new PreviewSystem(width, height, useSharedMemory);
        _graphicsInitializer = new GraphicsInitializer(GraphicsApi, RenderSettings);

        _windowFacade.CreateWindow();

        // Subscribe to window events
        _windowFacade.Load += OnLoad;
        _windowFacade.Update += OnUpdateFrame;
        _windowFacade.Render += OnRenderFrame;
        _windowFacade.Closing += OnClosing;
        _windowFacade.Resize += OnResize;

        _inputRouter = new InputRouter(Ui);
        _runtimeDataPusher = new RuntimeDataPusher(Ui, _profiler);
    }

    private void OnLoad()
    {
        // Input system
        var inputProvider = new Input.SilkInputProvider(_windowFacade.Window!);
        Input.Input.Initialize(inputProvider);

        // Теперь безопасно подписываемся
        _inputRouter.Initialize();
        Debug.Log("[SNEngineHost] Input system initialized (Silk.NET).");

        if (!_previewSystem.IsEnabled)
            _windowFacade.CenterWindow();

        // Graphics initialization
        _graphicsInitializer.Initialize(_windowFacade.Window!);

        // Asset managers
        AssetManager = new AssetManager(GraphicsDevice!);
        FileManager = new FileManager(GraphicsDevice!);

        AssetManagerInitialized?.Invoke(AssetManager);

        // Initial viewport
        _graphicsInitializer.SetViewport(_previewSystem.IsEnabled ? _previewSystem.GetWidth() : 1280,
                                         _previewSystem.IsEnabled ? _previewSystem.GetHeight() : 720);

        SceneManager = new SceneManager();
        Debug.Initialize();

        _previewSystem.Initialize();

        Debug.Log("SNEngineHost: All systems initialized successfully.");

        // UI Graphics Context
        _graphicsContext = new InternalGraphicsContext(
            () => GraphicsDevice,
            () => Renderer.ViewportWidth,
            () => Renderer.ViewportHeight
        );

        Ui?.Initialize(_graphicsContext);
        UiOverlay?.Initialize(_graphicsContext);

        OnInitialized?.Invoke();
    }

    private void OnUpdateFrame(double deltaTime)
    {
        if (_isDisposing) return;

        Engine.Time.Update(deltaTime);
        Input.Input.Update();

        _inputRouter.ProcessInput();

        _profiler.BeginUpdate();

        _profiler.Time("Update/Scene", () => SceneManager?.Update(deltaTime));
        _profiler.Time("Update/UI", () => Ui?.Update(deltaTime));

        _profiler.Time("Update/DialogueSystem", () => DialogueSystem.Update());

        _profiler.Time("Update/RuntimeDataPush", () => _runtimeDataPusher.PushData());

        _profiler.Time("Update/JSBridge", () => JavaScriptBridge?.ProcessPendingCalls());

        _profiler.EndUpdate();
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

        _profiler.Time("Render/UI", () =>
        {
            Ui?.Render(_graphicsContext);
            UiOverlay?.Render(_graphicsContext);
        });

        // Preview
        if (_previewSystem.IsEnabled)
        {
            _previewSystem.PublishFrame(_graphicsInitializer.GetGL());
        }

        _profiler.EndRenderAndMaybeLog();
    }

    private void OnResize(Vector2D<int> newSize)
    {
        if (newSize.X <= 0 || newSize.Y <= 0) return;

        _graphicsInitializer.SetViewport(newSize.X, newSize.Y);
        Debug.Log($"Window resized to {newSize.X}x{newSize.Y}");

        Ui?.Resize(newSize.X, newSize.Y);
        UiOverlay?.Resize(newSize.X, newSize.Y);

        Resized?.Invoke(newSize.X, newSize.Y);
    }

    private void OnClosing()
    {
        if (_isDisposing) return;
        _isDisposing = true;

        _windowFacade.Load -= OnLoad;
        _windowFacade.Update -= OnUpdateFrame;
        _windowFacade.Render -= OnRenderFrame;
        _windowFacade.Closing -= OnClosing;
        _windowFacade.Resize -= OnResize;

        Task.Run(SafeDispose);
    }

    private void SafeDispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _inputRouter.Dispose();
            _previewSystem.Dispose();
            _graphicsInitializer.Dispose();

            Ui?.Dispose();
            UiOverlay?.Dispose();

            AssetManager?.Dispose();
            Renderer?.Dispose();
            GraphicsDevice?.Dispose();

            _windowFacade.Dispose();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SNEngineHost] Dispose error: {ex.Message}");
        }
        finally
        {
            SceneManager = null!;
            FileManager = null!;
            _graphicsContext = null;
        }
    }

    public void Run() => _windowFacade.Run();

    public void Dispose()
    {
        SafeDispose();
        GC.SuppressFinalize(this);
    }

    // Public input forwarding API
    public void ProcessMouseMove(float x, float y) => _inputRouter.ProcessMouseMove(x, y);
    public void ProcessMouseButton(MouseButton button, bool isDown, float x, float y)
        => _inputRouter.ProcessMouseButton(button, isDown, x, y);
}