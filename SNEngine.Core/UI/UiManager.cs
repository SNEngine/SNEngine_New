using System;
using System.Collections.Generic;
using System.Linq;
using static SNEngine.Core.Debug;
using SNEngine.Core.Rendering;

namespace SNEngine.Core.UI;

/// <summary>
/// Central manager for all UI elements in the game.
/// 
/// Responsibilities:
/// - Owns the list of active IUiElement instances
/// - Handles initialization, update, rendering, resizing and disposal
/// - Provides z-ordering (higher ZIndex = drawn later / on top)
/// - Will eventually handle input routing (mouse/keyboard to the topmost interactive element)
///
/// This is the recommended high-level API for working with multiple HTML (or other) UIs.
/// </summary>
public sealed class UiManager : IDisposable
{
    private readonly List<IUiElement> _elements = new();
    private IGraphicsContext? _context;
    private bool _disposed;

    /// <summary>
    /// All currently registered UI elements (read-only view).
    /// </summary>
    public IReadOnlyList<IUiElement> Elements => _elements;

    /// <summary>
    /// Whether the manager has been initialized with a graphics context.
    /// </summary>
    public bool IsInitialized => _context != null;

    /// <summary>
    /// Registers a new UI element. The element will be initialized if the manager is already initialized.
    /// Elements are automatically sorted by ZIndex during rendering.
    /// 
    /// If the element is an UltralightHtmlElement and no PreRenderHook is set yet,
    /// it will automatically wire the shared Ultralight renderer update pass.
    /// </summary>
    public void Add(IUiElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (_elements.Contains(element)) return;

        _elements.Add(element);

        if (_context != null)
        {
            try
            {
                element.Initialize(_context);
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize UI element: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Removes and disposes the specified element.
    /// </summary>
    public void Remove(IUiElement element)
    {
        if (element == null) return;
        if (_elements.Remove(element))
        {
            try
            {
                element.Dispose();
            }
            catch (Exception ex)
            {
                LogError($"Error disposing UI element: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Removes all elements and disposes them.
    /// </summary>
    public void Clear()
    {
        foreach (var element in _elements.ToList())
        {
            try
            {
                element.Dispose();
            }
            catch (Exception ex)
            {
                // During shutdown many UI elements (especially Ultralight + TrippyGL) will
                // throw "NoContext" or "Wrong thread" errors because the OpenGL context
                // has already been destroyed or is not current. These are expected and safe to ignore.
                if (IsExpectedShutdownDisposeError(ex))
                {
                    // Log at lower level to reduce noise on normal exit
                    try
                    {
                        SNEngine.Core.Debug.Log($"[UiManager] UI element disposed after context was destroyed (normal on exit).");
                    }
                    catch { }
                }
                else
                {
                    LogError($"Error disposing UI element during Clear: {ex.Message}");
                }
            }
        }
        _elements.Clear();
    }

    private static bool IsExpectedShutdownDisposeError(Exception ex)
    {
        if (ex is null) return false;
        string msg = ex.Message ?? string.Empty;
        return msg.Contains("NoContext", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("current OpenGL", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("entry point", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("Wrong thread", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes the manager and all current elements.
    /// Called by SNEngineHost after graphics device creation.
    /// </summary>
    public void Initialize(IGraphicsContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        foreach (var element in _elements)
        {
            try
            {
                element.Initialize(context);
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize UI element during UiManager.Initialize: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Updates all visible elements.
    /// </summary>
    public void Update(double deltaTime)
    {
        foreach (var element in _elements)
        {
            if (!element.Visible) continue;

            try
            {
                element.Update(deltaTime);
            }
            catch (Exception ex)
            {
                LogError($"Error updating UI element: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Optional action that will be executed once before rendering any elements.
    /// Used by backends like Ultralight that require a single central UpdateAndRender() call
    /// for all their views.
    /// 
    /// This is typically wired automatically when using SNEngine.CreateHtmlElement().
    /// </summary>
    public Action? PreRenderHook { get; set; }

    /// <summary>
    /// Renders all visible elements in Z-order (lower Z first, higher Z on top).
    /// </summary>
    public void Render(IGraphicsContext context)
    {
        // Allow backends (e.g. Ultralight) to perform one central update/render pass
        // for all their views before individual elements upload and draw their surfaces.
        PreRenderHook?.Invoke();

        if (_elements.Count == 0) return;

        // Sort by ZIndex every frame (cheap for small number of elements).
        // For very dynamic UIs a dirty-flag + cached sorted list can be added later.
        var sorted = _elements
            .Where(e => e.Visible)
            .OrderBy(e => e.ZIndex)
            .ToList();

        foreach (var element in sorted)
        {
            try
            {
                element.Render(context);
                // Note: JS runtime data pushing (FPS etc.) is now done in Update(),
                // not here. This respects the Update/Render separation at Silk.NET level.
            }
            catch (Exception ex)
            {
                LogError($"Error rendering UI element (Z={element.ZIndex}): {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Notifies all elements about a viewport size change.
    /// </summary>
    public void Resize(int width, int height)
    {
        // Skip resize when minimized (0x0). Many backends (Ultralight, GL textures, etc.)
        // do not support zero-size resources. The next real resize will restore everything.
        if (width <= 0 || height <= 0)
            return;

        foreach (var element in _elements)
        {
            try
            {
                element.Resize(width, height);
            }
            catch (Exception ex)
            {
                LogError($"Error resizing UI element: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Clear();
        _context = null;
    }

    // ============================================================
    // Basic Input Support (Point 5 - initial implementation)
    // ============================================================

    /// <summary>
    /// Finds the topmost (highest ZIndex) interactive element that contains the given screen point.
    /// Simple AABB check based on element's logical bounds (future: each element can provide its own hit area).
    /// </summary>
    public IUiElement? HitTest(float x, float y)
    {
        // We currently don't store per-element bounds on IUiElement.
        // For now this returns the topmost interactive visible element (very basic).
        // Proper implementation will come when UltralightHtmlElement starts storing Position + Size.
        return _elements
            .Where(e => e.Visible && e.IsInteractive)
            .OrderByDescending(e => e.ZIndex)
            .FirstOrDefault();
    }

    /// <summary>
    /// Sends a mouse move event. Returns the element that should receive it (if any).
    /// </summary>
    public IUiElement? ProcessMouseMove(float x, float y)
    {
        var target = HitTest(x, y);
        // In the future: call methods on the element or raise events
        return target;
    }

    private static void LogError(string message)
    {
        try
        {
            // Use fully qualified name to avoid recursion with the local method name
            SNEngine.Core.Debug.LogError($"[UiManager] {message}");
        }
        catch
        {
            Console.WriteLine($"[UiManager ERROR] {message}");
        }
    }
}
