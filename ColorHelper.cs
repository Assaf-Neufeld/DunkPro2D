using Microsoft.Xna.Framework;

namespace DunkPro2D;

/// <summary>
/// Helper class for Color creation to avoid constructor ambiguity in MonoGame.
/// </summary>
public static class ColorHelper
{
    public static Color FromRgba(int r, int g, int b, int a)
    {
        return new Color((byte)r, (byte)g, (byte)b, (byte)a);
    }
}
