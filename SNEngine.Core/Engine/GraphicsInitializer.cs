using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SNEngine.Core.Rendering;
using System;
using TrippyGL;

namespace SNEngine.Core.Engine;

/// <summary>
/// Responsible for graphics context and rendering system initialization.
/// Handles GraphicsDevice, Renderer, and related graphics setup.
/// </summary>
public class GraphicsInitializer : IDisposable
{
    private GL? _gl;
    private readonly GraphicsAPI _graphicsApi;
    private readonly RenderSettings _renderSettings;

    public GraphicsDevice? GraphicsDevice { get; private set; }
    public Renderer? Renderer { get; private set; }

    public GraphicsInitializer(GraphicsAPI graphicsApi, RenderSettings? renderSettings = null)
    {
        _graphicsApi = graphicsApi;
        _renderSettings = renderSettings ?? new RenderSettings();
    }

    /// <summary>
    /// Initializes OpenGL context, TrippyGL GraphicsDevice and Renderer.
    /// </summary>
    public void Initialize(IWindow window)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));

        _gl = GL.GetApi(window);

        // Create TrippyGL GraphicsDevice
        GraphicsDevice = new GraphicsDevice(_gl);
        Console.WriteLine("[GraphicsInitializer] TrippyGL GraphicsDevice created.");

        // Apply render settings
        GraphicsDevice.ClearColor = _renderSettings.ClearColor;
        GraphicsDevice.BlendState = _renderSettings.BlendState;

        // Initialize Renderer
        Renderer = new Renderer();
        Renderer.Initialize(GraphicsDevice, _renderSettings);

        Debug.Log("GraphicsInitializer: Graphics systems initialized successfully.");
    }

    /// <summary>
    /// Updates viewport size after window resize.
    /// </summary>
    public void SetViewport(int width, int height)
    {
        Renderer?.SetViewport(width, height);
    }

    /// <summary>
    /// Returns the underlying OpenGL API instance.
    /// </summary>
    public GL? GetGL() => _gl;

    public void Dispose()
    {
        try
        {
            if (Renderer != null)
            {
                Renderer.Dispose();
                Renderer = null;
            }

            if (GraphicsDevice != null)
            {
                GraphicsDevice.Dispose();
                GraphicsDevice = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GraphicsInitializer] Dispose error: {ex.Message}");
        }
    }
}