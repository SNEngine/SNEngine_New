using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SNEngine.Assets.Package;
using SNEngine.Core.Assets;
using UltralightNet;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Handles loading HTML content into an Ultralight View.
/// Supports asset loading with caching and notifies SnpkFileSystem about current screen context.
/// </summary>
public class UltralightHtmlLoader
{
    // Static cache for HTML content (shared between all elements)
    private static readonly Dictionary<string, string> _htmlCache =
        new(StringComparer.OrdinalIgnoreCase);

    // Cache for post-inlined (assets turned into data: URIs) HTML.
    // Keyed by the html asset path (for LoadScreen paths this includes the screen name so is unique).
    // Avoids re-running regex + asset resolution on repeated loads of the same screen (hot reload, re-entry, etc.).
    private static readonly Dictionary<string, string> _processedHtmlCache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Reference to our custom file system to set screen context.
    /// </summary>
    private static SnpkFileSystem? _snpkFileSystem;

    /// <summary>
    /// Sets the custom SnpkFileSystem instance (called once during initialization).
    /// </summary>
    public static void SetSnpkFileSystem(SnpkFileSystem fileSystem)
    {
        _snpkFileSystem = fileSystem;
    }

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
            // Set context BEFORE inlining + HTML assignment so relative resolution (e.g. media/) works for inliner
            if (_snpkFileSystem != null)
            {
                _snpkFileSystem.SetCurrentScreen(ulView, screenName);
            }

            // Check processed (inlined) cache first. For LoadScreen the htmlPath already encodes the screen
            // ("ui/{screenName}/index.html"), so the inlined result (with resolved media/ etc.) is stable per screen.
            string processedKey = htmlPath;
            if (_processedHtmlCache.TryGetValue(processedKey, out var processed))
            {
                htmlContent = processed;
            }
            else
            {
                // Inline local assets (img src="media/...", style url(...) etc) as data: URIs.
                // This ensures <img> and similar from packaged HTML work reliably even if
                // Ultralight does not query IFileSystem for relative paths in .HTML-set content.
                htmlContent = InlineLocalAssets(htmlContent, _snpkFileSystem);
                if (!string.IsNullOrEmpty(htmlContent))
                {
                    _processedHtmlCache[processedKey] = htmlContent;
                }
            }

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
            // Set context first (for relative asset resolution during inlining)
            string screenName = "";
            if (_snpkFileSystem != null)
            {
                screenName = ExtractScreenName(assetPath);
                if (!string.IsNullOrEmpty(screenName))
                {
                    _snpkFileSystem.SetCurrentScreen(ulView, screenName);
                }
            }

            // Compute normalized path for cache key (same logic as internal loader)
            string normalized = assetPath.Replace('\\', '/').TrimStart('/');
            string processedKey = normalized;
            if (!string.IsNullOrEmpty(screenName))
                processedKey += "|" + screenName;

            if (_processedHtmlCache.TryGetValue(processedKey, out var processed))
            {
                htmlContent = processed;
            }
            else
            {
                htmlContent = InlineLocalAssets(htmlContent, _snpkFileSystem);
                if (!string.IsNullOrEmpty(htmlContent))
                {
                    _processedHtmlCache[processedKey] = htmlContent;
                }
            }

            ulView.HTML = htmlContent;
        }
    }

    /// <summary>
    /// Attempts to extract screen name from path (e.g. "ui/dialog/index.html" → "dialog")
    /// </summary>
    private static string ExtractScreenName(string assetPath)
    {
        string normalized = assetPath.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("ui/"))
        {
            var parts = normalized.Split('/');
            if (parts.Length >= 2)
                return parts[1];
        }
        return "";
    }

    /// <summary>
    /// Internal helper with caching.
    /// </summary>
    private string? LoadHtmlFromAssetInternal(AssetManager assetManager, string assetPath, AssetType assetType = AssetType.UI)
    {
        string normalizedPath = assetPath.Replace('\\', '/').TrimStart('/');

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
        _processedHtmlCache.Clear();
    }

    /// <summary>
    /// Rewrites the HTML to embed local assets (referenced via relative paths like "media/foo.png")
    /// as data: URIs using the SnpkFileSystem resolution (which knows the current screen).
    /// This guarantees that &lt;img&gt;, CSS url(), etc. work inside UI screens loaded from .snpk
    /// even though Ultralight may not invoke the custom IFileSystem for subresources when HTML
    /// content is injected directly via the .HTML setter.
    /// </summary>
    private static string InlineLocalAssets(string html, SnpkFileSystem? fs)
    {
        if (string.IsNullOrEmpty(html) || fs == null)
            return html;

        // 1. src="..." or src='...' (img, script, link, etc.)
        html = Regex.Replace(html, @"(src|href)\s*=\s*[""'](?<path>[^""'#>]+?)[""']", m =>
        {
            string attr = m.Groups[1].Value;
            string p = m.Groups["path"].Value.Trim();
            if (IsSkippableRef(p))
                return m.Value;

            byte[]? data = fs.ResolveAsset(p);
            if (data != null && data.Length > 0)
            {
                string mime = GuessMime(p);
                string b64 = Convert.ToBase64String(data);
                return $"{attr}=\"data:{mime};base64,{b64}\"";
            }
            return m.Value;
        });

        // 2. url(...) inside style="" or <style> blocks (background, @font-face, etc.)
        html = Regex.Replace(html, @"url\s*\(\s*[""']?(?<path>[^""')#>\s]+?)[""']?\s*\)", m =>
        {
            string p = m.Groups["path"].Value.Trim();
            if (IsSkippableRef(p))
                return m.Value;

            byte[]? data = fs.ResolveAsset(p);
            if (data != null && data.Length > 0)
            {
                string mime = GuessMime(p);
                string b64 = Convert.ToBase64String(data);
                return $"url(\"data:{mime};base64,{b64}\")";
            }
            return m.Value;
        });

        return html;
    }

    private static bool IsSkippableRef(string p)
    {
        if (string.IsNullOrWhiteSpace(p)) return true;
        if (p.Contains("://")) return true; // http, https, etc.
        if (p.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return true;
        if (p.StartsWith("#")) return true; // anchors
        if (p.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static string GuessMime(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant().TrimStart('.');
        return ext switch
        {
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "gif" => "image/gif",
            "webp" => "image/webp",
            "svg" => "image/svg+xml",
            "css" => "text/css",
            "js" => "application/javascript",
            "json" => "application/json",
            "woff" => "font/woff",
            "woff2" => "font/woff2",
            "ttf" => "font/ttf",
            "otf" => "font/otf",
            _ => "application/octet-stream"
        };
    }
}