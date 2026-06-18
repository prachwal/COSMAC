using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace Cdp1802.Gui.Controls;

public class MemoryHeatmap : Control
{
    public static readonly StyledProperty<uint[]?> HeatProperty =
        AvaloniaProperty.Register<MemoryHeatmap, uint[]?>(nameof(Heat));

    public uint[]? Heat
    {
        get => GetValue(HeatProperty);
        set => SetValue(HeatProperty, value);
    }

    private WriteableBitmap? _bitmap;

    public MemoryHeatmap()
    {
        AffectsRender<MemoryHeatmap>(HeatProperty);
    }

    public override void Render(DrawingContext ctx)
    {
        var heat = Heat;
        if (heat is null) return;

        _bitmap ??= new(new PixelSize(256, 256), new Vector(96, 96), PixelFormat.Bgra8888);

        var bounds = Bounds;
        double cw = bounds.Width / 256;
        double ch = bounds.Height / 256;

        uint max = 1;
        foreach (var h in heat)
            if (h > max) max = h;

        using var fb = _bitmap.Lock();
        unsafe
        {
            uint* ptr = (uint*)fb.Address;
            for (int i = 0; i < 65536; i++)
            {
                if (heat[i] == 0)
                {
                    ptr[i] = 0xFF1A1A1A;
                    continue;
                }

                double t = Math.Log(1 + heat[i]) / Math.Log(1 + max);
                var (r, g, b) = LerpColor(t);
                ptr[i] = 0xFF000000u | ((uint)b << 16) | ((uint)g << 8) | (uint)r;
            }
        }

        ctx.DrawImage(_bitmap, new Rect(bounds.Size));
    }

    private static (byte r, byte g, byte b) LerpColor(double t)
    {
        byte r = (byte)(0x09 + (0xF8 - 0x09) * t);
        byte g = (byte)(0x69 + (0x51 - 0x69) * t);
        byte b = (byte)(0xDA + (0x49 - 0xDA) * t);
        return (r, g, b);
    }
}
