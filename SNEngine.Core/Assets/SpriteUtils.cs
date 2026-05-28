using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SNEngine.Core.Assets;

/// <summary>
/// Helper methods for working with 2D sprites (especially character standups).
/// </summary>
public static class SpriteUtils
{
    /// <summary>
    /// Computes "bounce" value from raw image data:
    /// distance in pixels from the very bottom of the image up to the lowest row
    /// that contains at least one pixel with alpha >= threshold.
    ///
    /// This value is used for automatic "grounded" positioning of characters
    /// so that their feet never get cut off at the bottom of the screen.
    /// Works exactly like setting a custom pivot at the feet in Unity.
    /// </summary>
    public static float ComputeBounce(Image<Rgba32> image, float alphaThreshold = 0.08f)
    {
        if (image == null || image.Height == 0)
            return 0f;

        for (int y = image.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (image[x, y].A / 255f >= alphaThreshold)
                {
                    return image.Height - 1 - y;
                }
            }
        }

        return 0f;
    }
}
