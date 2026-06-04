using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SNEngine.Core.Assets;
using SNEngine.Core.Engine.Systems;
using SNEngine.Core.Engine.Systems.DialogSystem;
using SNEngine.Core.Input;
using SNEngine.Core.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TrippyGL;

namespace SNEngine.Core.Engine;

/// <summary>
/// Main engine host with support for normal window mode and Shared Memory preview.
/// Automatically creates and registers all ISystem implementations.
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

    // Collection of all registered systems
    private readonly Dictionary<Type, ISystem> _systems = new();

    /// <summary>
    /// Static reference to the current engine host instance.
    /// </summary>
    public static SNEngineHost? Current { get; private set; }

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

        // Set static Current reference
        Current = this;

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

        // === Automatic ISystem creation and registration ===
        RegisterAllSystems();

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

    /// <summary>
    /// Dynamically loads optional SNEngine.* modules (e.g. SNEngine.Audio.dll + fmod.dll)
    /// if they are physically present next to the executable. This enables ISystem discovery
    /// (including IAudioSystem) without any compile-time ProjectReference from Runtime/Core/Test
    /// to the audio implementation.
    /// </summary>
    private static void LoadOptionalModulesIfPresent()
    {
        try
        {
            string baseDir = AppContext.BaseDirectory;
            // List of optional modules that can provide ISystem implementations
            string[] optionalModules = { "SNEngine.Audio.dll" };

            foreach (var moduleName in optionalModules)
            {
                string path = Path.Combine(baseDir, moduleName);
                if (!File.Exists(path))
                    continue;

                // Check if already loaded
                bool alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies()
                    .Any(a =>
                    {
                        var n = a.GetName().Name;
                        return n != null && n.Equals(Path.GetFileNameWithoutExtension(moduleName), StringComparison.OrdinalIgnoreCase);
                    });

                if (alreadyLoaded)
                    continue;

                var asm = Assembly.LoadFrom(path);
                Debug.Log($"[SNEngineHost] Dynamically loaded optional module: {moduleName} (for ISystem discovery)");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SNEngineHost] Failed to load optional module(s): {ex.Message}");
        }
    }

    /// <summary>
    /// Automatically discovers and registers all ISystem implementations.
    /// Scans executing assembly + other loaded SNEngine.* assemblies at runtime.
    /// This allows SNEngine.Audio (and future modules) to provide implementations of interfaces
    /// such as IAudioSystem without SNEngine.Core taking a compile-time project reference to them.
    /// </summary>
    private void RegisterAllSystems()
    {
        LoadOptionalModulesIfPresent();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Where(a =>
            {
                var name = a.GetName().Name;
                return name != null && (name.Equals("SNEngine.Core", StringComparison.Ordinal) || name.StartsWith("SNEngine.", StringComparison.Ordinal));
            })
            .ToArray();

        var systemTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                typeof(ISystem).IsAssignableFrom(t) &&
                t != typeof(ISystem)) // exclude the interface itself
            .Distinct()
            .ToList();

        Debug.Log($"[SNEngineHost] Found {systemTypes.Count} ISystem implementations across {assemblies.Length} assemblies.");

        foreach (var type in systemTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is ISystem system)
                {
                    RegisterSystemInternal(type, system);
                    _inputRouter.RegisterSystem(system);
                    Debug.Log($"[SNEngineHost] Registered system: {system.SystemName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SNEngineHost] Failed to create system {type.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Registers a system instance both under its concrete type and under all
    /// ISystem-derived interfaces it implements. This enables resolution by interface
    /// (e.g. GetSystem&lt;IAudioSystem&gt;()) for optional modules like SNEngine.Audio.
    /// </summary>
    private void RegisterSystemInternal(Type concreteType, ISystem system)
    {
        // Always register by the concrete implementation type
        _systems[concreteType] = system;

        // Also register under every interface that derives from ISystem.
        // This is what makes GetSystem<IAudioSystem>() succeed when the actual
        // instance is AudioSystem from a dynamically loaded assembly.
        foreach (var iface in concreteType.GetInterfaces())
        {
            if (typeof(ISystem).IsAssignableFrom(iface) && iface != typeof(ISystem))
            {
                // Last writer wins in case of multiple implementations for the same interface.
                _systems[iface] = system;
            }
        }
    }

    /// <summary>
    /// Gets a registered system by type.
    /// Supports both concrete types (e.g. DialogueSystem) and interfaces (e.g. IAudioSystem).
    /// </summary>
    public T? GetSystem<T>() where T : class, ISystem
    {
        var type = typeof(T);

        // Fast path: exact key (works for concretes and for interfaces we pre-registered)
        if (_systems.TryGetValue(type, out var system))
            return system as T;

        // Fallback: find any registered system that implements/derives from T.
        // This makes GetSystem<IAudioSystem>() work even without pre-indexing interfaces.
        foreach (var sys in _systems.Values)
        {
            if (sys is T match)
                return match;
        }

        return null;
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

        // Update all registered ISystems
        _profiler.Time("Update/Systems", () => _inputRouter.UpdateSystems(deltaTime));

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
            // Dispose any systems that implement IDisposable (e.g. AudioSystem releases FMOD)
            try
            {
                foreach (var sys in _systems.Values)
                {
                    if (sys is IDisposable d)
                        d.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SNEngineHost] Error disposing systems: {ex.Message}");
            }

            SceneManager = null!;
            FileManager = null!;
            _graphicsContext = null;
            _systems.Clear();
            Current = null; // Clear static reference on dispose
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