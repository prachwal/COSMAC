namespace Cdp1802.Core;

/// <summary>
/// Video renderer for CDP1861 Pixie.
/// Renders framebuffer to PPM image file.
/// </summary>
public class VideoRenderer
{
    private readonly Cdp1861 _pixie;

    /// <summary>
    /// Pixel scale (1 = 1 pixel, 2 = 2x2 pixels, etc.).
    /// </summary>
    public int Scale { get; set; } = 4;

    /// <summary>
    /// Foreground color (R, G, B).
    /// </summary>
    public (byte R, byte G, byte B) ForegroundColor { get; set; } = (0, 255, 0);

    /// <summary>
    /// Background color (R, G, B).
    /// </summary>
    public (byte R, byte G, byte B) BackgroundColor { get; set; } = (0, 0, 0);

    public VideoRenderer(Cdp1861 pixie)
    {
        _pixie = pixie;
    }

    /// <summary>
    /// Render framebuffer to PPM file.
    /// </summary>
    public void RenderToFile(string filename)
    {
        int width = _pixie.Width * Scale;
        int height = _pixie.Height * Scale;

        using var writer = new StreamWriter(filename);
        writer.WriteLine("P3");
        writer.WriteLine($"{width} {height}");
        writer.WriteLine("255");

        for (int y = 0; y < _pixie.Height; y++)
        {
            for (int sy = 0; sy < Scale; sy++)
            {
                for (int x = 0; x < _pixie.Width; x++)
                {
                    bool pixel = _pixie.GetPixel(x, y);
                    var color = pixel ? ForegroundColor : BackgroundColor;

                    for (int sx = 0; sx < Scale; sx++)
                        writer.Write($"{color.R} {color.G} {color.B} ");
                }
                writer.WriteLine();
            }
        }
    }

    /// <summary>
    /// Render to ASCII art string.
    /// </summary>
    public string RenderToAscii()
    {
        var sb = new System.Text.StringBuilder();

        for (int y = 0; y < _pixie.Height; y++)
        {
            for (int x = 0; x < _pixie.Width; x++)
            {
                sb.Append(_pixie.GetPixel(x, y) ? "█" : " ");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Render to console (colored).
    /// </summary>
    public void RenderToConsole()
    {
        for (int y = 0; y < _pixie.Height; y++)
        {
            for (int x = 0; x < _pixie.Width; x++)
            {
                if (_pixie.GetPixel(x, y))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("██");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write("  ");
                }
            }
            Console.WriteLine();
        }
        Console.ResetColor();
    }
}
