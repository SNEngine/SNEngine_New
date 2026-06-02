using System.IO;

namespace SNEngine.UI.Ultralight.FS;

/// <summary>
/// Helper class for determining MIME types and charsets for assets.
/// </summary>
public static class MimeTypeHelper
{
    public static string GetMimeType(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "application/octet-stream";

        string ext = Path.GetExtension(path).ToLowerInvariant();

        return ext switch
        {
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".woff2" => "font/woff2",
            ".woff" => "font/woff",
            ".ttf" => "font/ttf",
            ".otf" => "font/otf",
            ".txt" => "text/plain",
            ".dat" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }

    public static string GetCharset(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();

        return ext switch
        {
            ".html" or ".htm" or ".css" or ".js" or ".json" or ".txt" => "utf-8",
            _ => "binary"
        };
    }
}