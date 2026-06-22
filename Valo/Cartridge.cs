using System.Collections.Immutable;

namespace Valo;

public abstract class Cartridge
{
    public static Cartridge FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < 0x150) {
            throw new ArgumentException("Cartridge is missing header", nameof(data));
        }

        var type = data[0x147];

        return type switch {
            0x00 => new BarebonesCartridge(new Rom(data.ToImmutableArray())),
            _ => throw new NotSupportedException($"Cartridge type {type:X2} is not supported"),
        };
    }

    public abstract IEnumerable<LocatedMemory> MemoryLayout();
}

public sealed class BarebonesCartridge(Rom rom) : Cartridge
{
    public override IEnumerable<LocatedMemory> MemoryLayout() => [
        new(0, rom.Size, rom),
    ];
}
