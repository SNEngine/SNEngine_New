using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;

namespace SNEngine.Core.Engine;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FrameHeader
{
    public int Width;
    public int Height;
    public int Channels;
    public long FrameId;
    public long TimestampTicks;
    public int BufferIndex;
}

public sealed class SharedFramePublisher : IDisposable
{
    private const int HeaderSize = 32;
    private const int MaxWidth = 1920;
    private const int MaxHeight = 1080;
    private const int BufferSize = HeaderSize + (MaxWidth * MaxHeight * 4) * 2;

    private MemoryMappedFile? _mmf;
    private MemoryMappedViewAccessor? _accessor;
    private FileStream? _fileStream;
    private long _frameId;
    private int _currentBufferIndex;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public void Initialize(int width, int height)
    {
        if (_isInitialized) return;

        string tempPath = Path.Combine(Path.GetTempPath(), "SNEngine_Preview_Frame_v1.dat");

        _fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        _mmf = MemoryMappedFile.CreateFromFile(_fileStream, null, BufferSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
        _accessor = _mmf.CreateViewAccessor(0, BufferSize);

        WriteHeader(width, height);
        _isInitialized = true;
    }

    private void WriteHeader(int width, int height)
    {
        var header = new FrameHeader
        {
            Width = width,
            Height = height,
            Channels = 4,
            FrameId = 0,
            TimestampTicks = DateTime.UtcNow.Ticks,
            BufferIndex = 0
        };
        _accessor!.Write(0, ref header);
    }

    public void PublishFrame(int width, int height, ReadOnlySpan<byte> rgbaPixels)
    {
        if (!_isInitialized || _accessor == null) return;

        int bufferOffset = HeaderSize + (_currentBufferIndex * MaxWidth * MaxHeight * 4);
        int pixelDataSize = width * height * 4;

        var header = new FrameHeader
        {
            Width = width,
            Height = height,
            Channels = 4,
            FrameId = Interlocked.Increment(ref _frameId),
            TimestampTicks = DateTime.UtcNow.Ticks,
            BufferIndex = _currentBufferIndex
        };

        _accessor.Write(0, ref header);
        _accessor.WriteArray(bufferOffset, rgbaPixels.ToArray(), 0, Math.Min(pixelDataSize, rgbaPixels.Length));
        _currentBufferIndex = 1 - _currentBufferIndex;
    }

    public void Dispose()
    {
        _accessor?.Dispose();
        _mmf?.Dispose();
        _fileStream?.Dispose();
        _isInitialized = false;
    }
}