using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Color = Avalonia.Media.Color;
using Point = System.Drawing.Point;

namespace Valo.App;

public sealed class WritableBitmapLcd : ILcd
{
    private readonly int[] _buffer = new int[ILcd.Width * ILcd.Height];

    public void Poke(Point pixel, Shade shade)
    {
        var colour = shade switch {
            Shade.White     => Color.FromRgb(255, 255, 255),
            Shade.LightGrey => Color.FromRgb(128, 128, 128),
            Shade.DarkGrey  => Color.FromRgb(64, 64, 64),
            Shade.Black     => Color.FromRgb(0, 0, 0),
        };

        var offset = 4 * (pixel.Y * ILcd.Width + pixel.X);

        _buffer[offset] = (int)colour.ToUInt32();
    }

    public Bitmap DrainToBitmap()
    {
        var bitmap = new WriteableBitmap(
            size: new PixelSize(ILcd.Width, ILcd.Height),
            dpi: new Vector(96, 96),
            format: PixelFormat.Bgra8888,
            alphaFormat: AlphaFormat.Opaque
        );

        using (var frame = bitmap.Lock()) {
            Marshal.Copy(_buffer, 0, frame.Address, _buffer.Length);
        }

        Array.Clear(_buffer);
        return bitmap;
    }
}
