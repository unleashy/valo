using System.Drawing;

namespace Valo;

public interface ILcd
{
    public const int Width  = 160;
    public const int Height = 144;

    public void Poke(Point pixel, Shade shade);
    public void OnVBlank();
}

public enum Shade
{
    White,
    LightGrey,
    DarkGrey,
    Black,
}
