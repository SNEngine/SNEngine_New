using Silk.NET.OpenGL;
using SNEngine.Core.Assets;
using System;
using Texture = SNEngine.Core.Assets.Texture;

namespace SNEngine.Core.Rendering;

/// <summary>
/// Quad renderer. Background is always fullscreen.
/// </summary>
public unsafe class QuadRenderer : IDisposable
{
    private GL? _gl;
    private uint _vao, _vbo, _ebo;
    private uint _shaderProgram;

    public void Initialize(GL gl)
    {
        _gl = gl;
        CreateQuad();
        CreateShader();
        Console.WriteLine("[QuadRenderer] Initialized");
    }

    private void CreateQuad()
    {
        if (_gl == null) throw new InvalidOperationException("GL not initialized");

        // Простой fullscreen quad
        float[] vertices = {
            -1.0f,  1.0f,  0.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 1.0f
        };

        uint[] indices = { 0, 1, 2, 2, 3, 0 };

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);

        unsafe
        {
            fixed (float* v = vertices)
            {
                _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }

            fixed (uint* i = indices)
            {
                _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }
        }

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);

        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));

        _gl.BindVertexArray(0);
    }

    private void CreateShader()
    {
        if (_gl == null) return;

        const string vertexSrc = """
            #version 330 core
            layout (location = 0) in vec2 aPosition;
            layout (location = 1) in vec2 aTexCoord;
            out vec2 TexCoord;
            void main()
            {
                gl_Position = vec4(aPosition, 0.0, 1.0);
                TexCoord = aTexCoord;
            }
            """;

        const string fragmentSrc = """
            #version 330 core
            out vec4 FragColor;
            in vec2 TexCoord;
            uniform sampler2D uTexture;
            uniform float uAlpha = 1.0;
            void main()
            {
                FragColor = texture(uTexture, TexCoord) * vec4(1.0, 1.0, 1.0, uAlpha);
            }
            """;

        uint vs = CompileShader(vertexSrc, ShaderType.VertexShader);
        uint fs = CompileShader(fragmentSrc, ShaderType.FragmentShader);

        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vs);
        _gl.AttachShader(_shaderProgram, fs);
        _gl.LinkProgram(_shaderProgram);

        _gl.DeleteShader(vs);
        _gl.DeleteShader(fs);
    }

    private uint CompileShader(string source, ShaderType type)
    {
        if (_gl == null) return 0;
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        return shader;
    }

    /// <summary>
    /// Обычный draw (для спрайтов)
    /// </summary>
    public void Draw(Texture texture, float alpha = 1.0f)
    {
        DrawInternal(texture, alpha);
    }

    /// <summary>
    /// Специально для Background — всегда на весь экран
    /// </summary>
    public void DrawFullscreen(Texture texture, float alpha = 1.0f)
    {
        if (_gl == null || texture == null) return;

        _gl.UseProgram(_shaderProgram);
        texture.Bind();

        int alphaLoc = _gl.GetUniformLocation(_shaderProgram, "uAlpha");
        if (alphaLoc != -1) _gl.Uniform1(alphaLoc, alpha);

        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
        _gl.BindVertexArray(0);
    }
    private void DrawInternal(Texture texture, float alpha)
    {
        DrawFullscreen(texture, alpha); // пока всё через fullscreen
    }

    public void Dispose()
    {
        if (_gl == null) return;
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteProgram(_shaderProgram);
    }
}