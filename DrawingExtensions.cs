using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DunkPro2D;

/// <summary>
/// MonoGame helper extensions for drawing shapes.
/// </summary>
public static class DrawingExtensions
{
    private static Texture2D? _pixel;

    private static Texture2D GetPixel(SpriteBatch sb)
    {
        if (_pixel == null)
        {
            _pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }
        return _pixel;
    }

    public static void FillRectangle(this SpriteBatch sb, Rectangle rect, Color color)
    {
        sb.Draw(GetPixel(sb), rect, color);
    }

    public static void DrawRectangle(this SpriteBatch sb, Rectangle rect, Color color, int lineWidth)
    {
        var pixel = GetPixel(sb);

        // Top
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, lineWidth), color);
        // Bottom
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height - lineWidth, rect.Width, lineWidth), color);
        // Left
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, lineWidth, rect.Height), color);
        // Right
        sb.Draw(pixel, new Rectangle(rect.X + rect.Width - lineWidth, rect.Y, lineWidth, rect.Height), color);
    }
}
