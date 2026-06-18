using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace Cdp1802.Gui.Controls;

public class PixieDisplay : Control
{
    public static readonly StyledProperty<WriteableBitmap?> SourceProperty =
        AvaloniaProperty.Register<PixieDisplay, WriteableBitmap?>(nameof(Source));

    public WriteableBitmap? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public PixieDisplay()
    {
        AffectsRender<PixieDisplay>(SourceProperty);
    }

    public override void Render(DrawingContext ctx)
    {
        var src = Source;
        if (src == null) return;

        var bounds = Bounds;
        double scale = Math.Min(bounds.Width / src.Size.Width, bounds.Height / src.Size.Height);
        var destSize = new Size(src.Size.Width * scale, src.Size.Height * scale);
        var destRect = new Rect(
            (bounds.Width - destSize.Width) / 2,
            (bounds.Height - destSize.Height) / 2,
            destSize.Width, destSize.Height);

        ctx.DrawImage(src, destRect);
    }
}
