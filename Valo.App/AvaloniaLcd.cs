using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Color = Avalonia.Media.Color;
using Point = System.Drawing.Point;

namespace Valo.App;

public sealed class AvaloniaLcd : CompositionCustomVisualHandler, ILcd, IDisposable
{
    private readonly WriteableBitmap _bitmap = new(
        size: new PixelSize(ILcd.Width, ILcd.Height),
        dpi: new Vector(96, 96),
        format: PixelFormat.Bgra8888,
        alphaFormat: AlphaFormat.Opaque
    );

    private readonly uint[,] _buffer = new uint[ILcd.Height, ILcd.Width];

    public void Poke(Point pixel, Shade shade)
    {
        var colour = shade switch {
            Shade.White     => Color.FromRgb(255, 255, 255),
            Shade.LightGrey => Color.FromRgb(170, 170, 170),
            Shade.DarkGrey  => Color.FromRgb(85, 85, 85),
            Shade.Black     => Color.FromRgb(0, 0, 0),
        };

        _buffer[pixel.Y, pixel.X] = colour.ToUInt32();
    }

    public void OnVBlank()
    {
        using (var dst = _bitmap.Lock()) {
            unsafe {
                fixed (uint* src = _buffer) {
                    Unsafe.CopyBlock((void*)dst.Address, src, sizeof(uint) * (uint)_buffer.Length);
                }
            }
        }

        Array.Clear(_buffer);
    }

    public override void OnMessage(object message)
    {
        RegisterForNextAnimationFrameUpdate();
    }

    public override void OnRender(ImmediateDrawingContext drawingContext)
    {
        var size = _bitmap.PixelSize.ToSize(1);
        var bounds = GetRenderBounds();
        var scale = Stretch.Uniform.CalculateScaling(bounds.Size, size, StretchDirection.UpOnly);
        var scaledSize = size * scale;
        var destRect = new Rect(bounds.Size)
            .CenterRect(new Rect(scaledSize))
            .Intersect(new Rect(bounds.Size));

        drawingContext.DrawBitmap(_bitmap, sourceRect: new Rect(size), destRect: destRect);
    }

    public override void OnAnimationFrameUpdate()
    {
        Invalidate();
        RegisterForNextAnimationFrameUpdate();
    }

    public void Dispose()
    {
        _bitmap.Dispose();
    }
}
