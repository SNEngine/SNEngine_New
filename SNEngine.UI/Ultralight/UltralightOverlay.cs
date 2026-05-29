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

    private bool _isTransparent = true;

    public UltralightOverlay(AssetManager? assetManager = null, bool transparent = true)
    {
        _assetManager = assetManager;
        _isTransparent = transparent;
    }

    /// <summary>
    /// Whether the HTML view should render with a transparent background.
    /// When true, the game underneath will be visible through the UI.
    /// Set before Initialize (i.e. before Run()).
    /// </summary>
    public bool IsTransparent
    {
        get => _isTransparent;
        set => _isTransparent = value;
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
        // IsTransparent = true → HTML фон прозрачный, игра видна сквозь UI
        ULViewConfig viewConfig = new ULViewConfig 
        { 
            IsAccelerated = false,
            IsTransparent = _isTransparent
        };
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
        // During the transition, we still support the old single-view behavior.
        // In the long term, users should use SNEngine.Ui.CreateHtmlElement(...) instead.
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

        // Передаем сырой буфер напрямую в OpenGL текстуру TrippyGL.
        // (Сохранение/восстановление состояния делается шире — вокруг всего блока UI рендеринга)
        _context.GL.ActiveTexture(TextureUnit.Texture0);
        _context.GL.BindTexture(TextureTarget.Texture2D, _uiTexture.Handle);

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

        // ============================================================
        // 3. Отрисовка UI-текстуры поверх игры + ЖЁСТКОЕ восстановление состояния
        // ============================================================

        // Сохраняем критически важное состояние, которое может сломать TrippyGL
        var previousBlendState = _context.GraphicsDevice.BlendState;

        _context.GL.GetInteger(GetPName.ActiveTexture, out int prevActiveTexture);
        _context.GL.GetInteger(GetPName.TextureBinding2D, out int prevTexture);
        _context.GL.GetInteger(GetPName.CurrentProgram, out int prevProgram);
        _context.GL.GetInteger(GetPName.VertexArrayBinding, out int prevVAO);

        // Делаем UI рендеринг
        _context.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

        _uiBatcher.Begin(BatcherBeginMode.Deferred);
        _uiBatcher.Draw(_uiTexture, Vector2.Zero, null, Color4b.White, 1f, 0f, Vector2.Zero);
        _uiBatcher.End();

        // === ЖЁСТКОЕ ВОССТАНОВЛЕНИЕ СОСТОЯНИЯ ===
        // Это критично, потому что мы мешаем raw OpenGL и TrippyGL.
        // Без этого в fullscreen часто весь экран становится чёрным,
        // а персонажи — чёрными квадратами.

        // Восстанавливаем blend state
        _context.GraphicsDevice.BlendState = previousBlendState;

        // Восстанавливаем active texture unit и привязку
        _context.GL.ActiveTexture((TextureUnit)prevActiveTexture);
        _context.GL.BindTexture(TextureTarget.Texture2D, (uint)prevTexture);

        // Восстанавливаем shader program и VAO (TrippyGL очень зависит от этого)
        _context.GL.UseProgram((uint)prevProgram);
        _context.GL.BindVertexArray((uint)prevVAO);

        // Дополнительная страховка
        _context.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _context.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
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
        // TrippyGL GL objects (_uiTexture, _uiBatcher, _uiShader) require an active OpenGL context
        // to delete their resources. During normal application shutdown the context is already destroyed,
        // which causes Silk.NET to throw "NoContext". This is expected and safe to ignore.
        try
        {
            _uiTexture?.Dispose();
            _uiBatcher?.Dispose();
            _uiShader?.Dispose();
        }
        catch (Exception ex) when (IsNoContextError(ex))
        {
            // Expected on shutdown — OpenGL context has already been destroyed.
        }
        catch (Exception ex)
        {
            // Log unexpected errors at warning level so they don't spam as errors.
            SNEngine.Core.Debug.LogWarning($"[UltralightOverlay] Non-critical dispose error: {ex.Message}");
        }

        // Ultralight native objects are generally safer, but we still protect them.
        try
        {
            _ulView?.Dispose();
            _ulRenderer?.Dispose();
        }
        catch (Exception ex)
        {
            SNEngine.Core.Debug.LogWarning($"[UltralightOverlay] Ultralight dispose warning: {ex.Message}");
        }
    }

    private static bool IsNoContextError(Exception ex)
    {
        if (ex is null) return false;

        // Silk.NET throws this when trying to resolve GL entry points after context destruction.
        string message = ex.Message ?? string.Empty;
        return message.Contains("NoContext", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("current OpenGL", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("entry point", StringComparison.OrdinalIgnoreCase);
    }
}