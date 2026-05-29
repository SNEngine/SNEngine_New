using Silk.NET.OpenGL;
using SNEngine.Assets.Package;
using SNEngine.Core.Assets;
using SNEngine.Core.Rendering;
using System;
using System.Drawing;
using System.Numerics;
using TrippyGL;
using UltralightNet;
using UltralightNet.AppCore;

namespace SNEngine.UI.Ultralight;

public unsafe class UltralightOverlay : IUiOverlay
{
    private AssetManager? _assetManager;
    private IGraphicsContext? _context;
    private string? _currentScreen;

    // Ultralight компоненты
    private UltralightNet.Renderer? _ulRenderer;
    private View? _ulView;

    // Графические ресурсы TrippyGL
    private Texture2D? _uiTexture;
    private TextureBatcher? _uiBatcher;
    private SimpleShaderProgram? _uiShader;

    public UltralightOverlay(AssetManager? assetManager = null)
    {
        _assetManager = assetManager;
    }

    public void SetAssetManager(AssetManager assetManager) { _assetManager = assetManager; }

    public void Initialize(IGraphicsContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // 1. Настройка платформы (шрифты + файловая система) через AppCore
        AppCoreMethods.SetPlatformFontLoader();
        AppCoreMethods.ulEnablePlatformFileSystem(AppContext.BaseDirectory);

        // 2. Создание рендерера (API 1.3.0: через ULPlatform, не статический Create)
        ULConfig config = new ULConfig();
        _ulRenderer = ULPlatform.CreateRenderer(config);

        // 3. Создание View под размер экрана
        ULViewConfig viewConfig = new ULViewConfig { IsAccelerated = false };
        _ulView = _ulRenderer.CreateView((uint)_context.ViewportWidth, (uint)_context.ViewportHeight, viewConfig);

        // 4. Создание текстуры в TrippyGL (6-параметровый ctor: device, w, h, genMips, samples, format)
        _uiTexture = new Texture2D(_context.GraphicsDevice, (uint)_context.ViewportWidth, (uint)_context.ViewportHeight, false, 0, TextureImageFormat.Color4b);
        _uiTexture.SetTextureFilters(TrippyGL.TextureMinFilter.Linear, TrippyGL.TextureMagFilter.Linear);

        _uiBatcher = new TextureBatcher(_context.GraphicsDevice);
        _uiShader = SimpleShaderProgram.Create<VertexColorTexture>(_context.GraphicsDevice);
        _uiBatcher.SetShaderProgram(_uiShader);

        UpdateProjection(_context.ViewportWidth, _context.ViewportHeight);
    }

    public void LoadScreen(string screenName)
    {
        if (_ulView == null)
            return;

        // Empty / null name → clear the current screen
        if (string.IsNullOrWhiteSpace(screenName))
        {
            _ulView.HTML = string.Empty;
            _currentScreen = null;
            return;
        }

        if (_assetManager == null)
            return;

        string htmlPath = $"ui/{screenName}/index.html";
        string? htmlContent = _assetManager.LoadText(htmlPath, AssetType.UI);

        if (string.IsNullOrEmpty(htmlContent))
        {
            htmlContent = _assetManager.LoadText("index.html", AssetType.UI);
        }

        if (!string.IsNullOrEmpty(htmlContent))
        {
            _currentScreen = screenName;
            _ulView.HTML = htmlContent;
        }
    }

    public void Render(IGraphicsContext context)
    {
        if (_ulRenderer == null || _ulView == null || _uiTexture == null || _uiBatcher == null)
            return;

        // 1. Шаг обновления логики и рендеринга во внутренний буфер
        _ulRenderer.Update();
        _ulRenderer.Render();

        // 2. Получаем доступ к пикселям через прямые свойства Surface и Bitmap
        // Surface is ULSurface? (nullable struct) when IsAccelerated=false
        ULSurface? surface = _ulView.Surface;
        if (surface == null)
            return;

        ULBitmap bitmap = surface.Value.Bitmap;

        void* pixels = bitmap.LockPixels();

        // Передаем сырой буфер напрямую в OpenGL текстуру TrippyGL
        _context!.GL.BindTexture(TextureTarget.Texture2D, _uiTexture.Handle);
        _context.GL.TexSubImage2D(
            TextureTarget.Texture2D,
            0,
            0, 0,
            _uiTexture.Width, _uiTexture.Height,
            PixelFormat.Bgra,
            PixelType.UnsignedByte,
            pixels
        );

        bitmap.UnlockPixels();

        // 3. Отрисовка текстуры поверх игрового кадра
        _context.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

        _uiBatcher.Begin(BatcherBeginMode.Deferred);
        _uiBatcher.Draw(_uiTexture, Vector2.Zero, null, Color4b.White, 1f, 0f, Vector2.Zero);
        _uiBatcher.End();
    }

    public void Resize(int width, int height)
    {
        if (_ulView == null || _context == null)
            return;

        _ulView.Resize((uint)width, (uint)height);

        _uiTexture?.Dispose();
        // 6-параметровый ctor для Texture2D (samples = 0)
        _uiTexture = new Texture2D(_context.GraphicsDevice, (uint)width, (uint)height, false, 0, TextureImageFormat.Color4b);
        _uiTexture.SetTextureFilters(TrippyGL.TextureMinFilter.Linear, TrippyGL.TextureMagFilter.Linear);

        UpdateProjection(width, height);
    }

    private void UpdateProjection(int width, int height)
    {
        if (_uiShader == null) return;

        _uiShader.Projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        _uiShader.World = Matrix4x4.Identity;
        _uiShader.View = Matrix4x4.Identity;
    }

    public void Dispose()
    {
        _uiTexture?.Dispose();
        _uiBatcher?.Dispose();
        _uiShader?.Dispose();

        _ulView?.Dispose();
        _ulRenderer?.Dispose();
    }
}