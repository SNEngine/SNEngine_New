using SNEngine.Core.Assets;
using SNEngine.UI.Ultralight.FS;
using System;
using UltralightNet;
using UltralightNet.Platform;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Main entry point for file system operations in Ultralight.
/// Delegates heavy logic to specialized classes.
/// </summary>
public sealed class SnpkFileSystem : IFileSystem
{
    private readonly ScreenContextManager _contextManager;
    private readonly AssetPathResolver _pathResolver;
    private bool _disposed;

    public SnpkFileSystem(AssetManager assetManager)
    {
        _contextManager = new ScreenContextManager();
        _pathResolver = new AssetPathResolver(assetManager, _contextManager);
    }

    public void SetCurrentScreen(View view, string screenName)
    {
        _contextManager.SetCurrentScreen(view, screenName);
    }

    public bool FileExists(string path) => _pathResolver.Resolve(path) != null;

    public unsafe ULBuffer OpenFile(string path)
    {
        byte[]? data = _pathResolver.Resolve(path);
        if (data == null || data.Length == 0)
            return default;

        return ULBuffer.CreateFromDataCopy<byte>(data.AsSpan());
    }

    public string GetFileMimeType(string path) => MimeTypeHelper.GetMimeType(path);
    public string GetFileCharset(string path) => MimeTypeHelper.GetCharset(path);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _contextManager.Clear();
    }

    public byte[]? ResolveAsset(string path) => _pathResolver.Resolve(path);
}