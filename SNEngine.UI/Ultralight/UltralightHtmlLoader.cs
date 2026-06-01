using System;
using System.Collections.Generic;
using SNEngine.Assets.Package;
using SNEngine.Core.Assets;
using UltralightNet;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Handles loading HTML content into an Ultralight View.
/// Supports asset loading with caching, LoadScreen, LoadHtml, and LoadHtmlAsset.
/// Extracted from UltralightHtmlElement.
/// </summary>
public class UltralightHtmlLoader
{
    // Static cache for HTML content (shared between all elements)
    private static readonly Dictionary<string, string> _htmlCache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Loads a screen by convention (ui/{screenName}/index.html)
    /// </summary>
    public void LoadScreen(View? ulView, AssetManager? assetManager, string screenName)
    {
        if (ulView == null) return;

        if (string.IsNullOrWhiteSpace(screenName))
        {
            ulView.HTML = string.Empty;
            return;
        }

        if (assetManager == null) return;

        string htmlPath = $"ui/{screenName}/index.html";
        string? htmlContent = LoadHtmlFromAssetInternal(assetManager, htmlPath);

        // Fallback to root index.html
        if (string.IsNullOrEmpty(htmlContent))
        {
            htmlContent = LoadHtmlFromAssetInternal(assetManager, "index.html");
        }

        if (!string.IsNullOrEmpty(htmlContent))
        {
            ulView.HTML = htmlContent;
        }
    }

    /// <summary>
    /// Loads raw HTML string directly into the view.
    /// </summary>
    public void LoadHtml(View? ulView, string html)
    {
        if (ulView == null) return;
        ulView.HTML = html ?? string.Empty;
    }

    /// <summary>
    /// Loads HTML from asset package by path.
    /// </summary>
    public void LoadHtmlAsset(View? ulView, AssetManager? assetManager, string assetPath, AssetType assetType = AssetType.UI)
    {
        if (ulView == null || assetManager == null) return;

        string? htmlContent = LoadHtmlFromAssetInternal(assetManager, assetPath, assetType);

        if (!string.IsNullOrEmpty(htmlContent))
        {
            ulView.HTML = htmlContent;
        }
    }

    /// <summary>
    /// Internal helper with caching.
    /// </summary>
    private string? LoadHtmlFromAssetInternal(AssetManager assetManager, string assetPath, AssetType assetType = AssetType.UI)
    {
        string normalizedPath = assetPath.Replace('\\', '/').TrimStart('/');

        // Check cache
        if (_htmlCache.TryGetValue(normalizedPath, out var cached))
            return cached;

        string? content = assetManager.LoadText(normalizedPath, assetType);

        if (!string.IsNullOrEmpty(content))
        {
            _htmlCache[normalizedPath] = content;
        }

        return content;
    }

    /// <summary>
    /// Clears the HTML cache (useful for development or hot reload).
    /// </summary>
    public static void ClearCache()
    {
        _htmlCache.Clear();
    }
}