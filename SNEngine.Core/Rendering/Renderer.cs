
using Silk.NET.OpenGL;
using SNEngine.Core.Components;
using SNEngine.Core.Scenes;
using System.Collections.Generic;

namespace SNEngine.Core.Rendering;

/// <summary>
/// Main renderer. Uses QuadRenderer for actual drawing.
/// GL is injected after window Load.
/// </summary>
public class Renderer : IDisposable
{
    private GL? _gl;
    private readonly QuadRenderer _quadRenderer;

    private readonly List<GameObject> _gameObjects = new();

    public Renderer()
    {
        _quadRenderer = new QuadRenderer();
    }

    /// <summary>
    /// Called from SNEngineHost.OnLoad when GL becomes available
    /// </summary>
    public void Initialize(GL gl)
    {
        _gl = gl;
        _quadRenderer.Initialize(gl);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.ClearColor(0.02f, 0.02f, 0.05f, 1.0f);

        Console.WriteLine("[Renderer] Initialized with OpenGL context.");
    }

    public void Begin()
    {
        _gameObjects.Clear();
    }

    public void DrawGameObject(GameObject gameObject)
    {
        if (gameObject?.Active == true)
            _gameObjects.Add(gameObject);
    }

    public void End()
    {
        foreach (var go in _gameObjects)
        {
            go.Render(this);
        }
    }

    public void Clear()
    {
        _gl?.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void DrawTexture(SNEngine.Core.Assets.Texture texture, float alpha = 1.0f)
    {
        _quadRenderer.Draw(texture, alpha);
    }

    public void Dispose()
    {
        _quadRenderer.Dispose();
    }
}