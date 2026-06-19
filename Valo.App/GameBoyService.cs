using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Text.Unicode;

namespace Valo.App;

public class GameBoyService
{
    public void LoadCartridge(ReadOnlySpan<byte> cartridge)
    {
        var logo = cartridge[0x104 .. 0x134];

        for (var i = 0; i < 3; i++) {
            for (var j = 0; j < 16; j++) {
                Console.Write($"{logo[16 * i + j]:X2} ");
            }

            Console.WriteLine();
        }
    }
}
