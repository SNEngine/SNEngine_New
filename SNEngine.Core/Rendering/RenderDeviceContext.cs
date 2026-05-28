using TrippyGL;
using SNEngine.Core.Engine;

namespace SNEngine.Core.Rendering;

/// <summary>
/// Encapsulates low-level graphics device ownership and state management.
/// This class owns the GraphicsDevice, main TextureBatcher, and shader program.
/// It is responsible for applying render states (blend, clear color) and basic device operations.
/// </summary>
public sealed class RenderDeviceContext : IDisposable
{
    private bool _disposed;

    public GraphicsDevice Device { get; }
    public TextureBatcher MainBatcher { get; }
    public SimpleShaderProgram SpriteShader { get; }

    public RenderDeviceContext(GraphicsDevice device, RenderSettings? initialSettings = null)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));

        MainBatcher = new TextureBatcher(device);
        SpriteShader = SimpleShaderProgram.Create<VertexColorTexture>(device);
        MainBatcher.SetShaderProgram(SpriteShader);

        // Apply initial settings if provided
        if (initialSettings != null)
        {
            ApplySettings(initialSettings);
        }
    }

    /// <summary>
    /// Applies blend state and clear color from the given settings.
    /// </summary>
    public void ApplySettings(RenderSettings settings)
    {
        if (settings == null) return;

        Device.BlendState = settings.BlendState;
        Device.ClearColor = settings.ClearColor;
    }

    public void SetViewport(int x, int y, int width, int height)
    {
        Device.SetViewport(x, y, (uint)width, (uint)height);
    }

    public void Clear()
    {
        Device.Clear(ClearBuffers.Color);
    }

    public void BeginMainBatch(BatcherBeginMode mode = BatcherBeginMode.Deferred)
    {
        MainBatcher.Begin(mode);
    }

    public void EndMainBatch()
    {
        MainBatcher.End();
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            MainBatcher?.Dispose();
            SpriteShader?.Dispose();

            // Note: We intentionally do NOT dispose the GraphicsDevice here.
            // Ownership of the device belongs to a higher level (usually SNEngineHost).
        }
        finally
        {
            _disposed = true;
        }
    }
}
