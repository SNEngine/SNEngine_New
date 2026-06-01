using System;
using System.Buffers;
using Silk.NET.OpenGL;
using SNEngine.Core.Engine;

namespace SNEngine.Core.Engine;

/// <summary>
/// Manages shared memory preview functionality for external viewers 
/// (e.g. editor preview window). Handles frame readback and publishing.
/// </summary>
public class PreviewSystem : IDisposable
{
    private SharedFramePublisher? _sharedFramePublisher;
    private byte[]? _previewPixelBuffer;

    private readonly int _width;
    private readonly int _height;
    private readonly bool _isEnabled;

    public bool IsEnabled => _isEnabled;

    public PreviewSystem(int width, int height, bool enabled = false)
    {
        _width = width;
        _height = height;
        _isEnabled = enabled;
    }

    /// <summary>
    /// Initializes the preview system if enabled.
    /// </summary>
    public void Initialize()
    {
        if (!_isEnabled) return;

        _sharedFramePublisher = new SharedFramePublisher();
        _sharedFramePublisher.Initialize(_width, _height);

        // Rent reusable buffer for ReadPixels (performance critical)
        int bufferSize = _width * _height * 4;
        _previewPixelBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        Debug.Log("[PreviewSystem] Shared Memory Publisher initialized successfully.");
    }

    /// <summary>
    /// Publishes current frame to shared memory for external preview.
    /// Should be called at the end of the render loop.
    /// </summary>
    public unsafe void PublishFrame(GL? gl)
    {
        if (!_isEnabled || _sharedFramePublisher == null || _previewPixelBuffer == null || gl == null)
            return;

        try
        {
            int neededSize = _width * _height * 4;
            byte[] pixels = _previewPixelBuffer;

            fixed (byte* ptr = pixels)
            {
                gl.ReadPixels(0, 0, (uint)_width, (uint)_height,
                    PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }

            _sharedFramePublisher.PublishFrame(_width, _height, pixels.AsSpan(0, neededSize));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PreviewSystem] Failed to publish frame: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns the preview width.
    /// </summary>
    public int GetWidth() => _width;

    /// <summary>
    /// Returns the preview height.
    /// </summary>
    public int GetHeight() => _height;

    public void Dispose()
    {
        if (_sharedFramePublisher != null)
        {
            try { _sharedFramePublisher.Dispose(); }
            catch { }
            _sharedFramePublisher = null;
        }

        if (_previewPixelBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_previewPixelBuffer);
            _previewPixelBuffer = null;
        }
    }
}