using System.Collections.Immutable;

namespace Valo;

public sealed class Rom : ISizedMemory
{
    private readonly ReadOnlyMemory<byte> _bytes;

    public Rom(ReadOnlyMemory<byte> bytes)
    {
        _bytes = bytes;
    }

    public Rom(ImmutableArray<byte> bytes)
    {
        _bytes = bytes.AsMemory();
    }

    public byte Read(ushort address) => _bytes.Span[address];

    public void Write(ushort address, byte value)
    {
        throw new InvalidOperationException(
            $"Write to read-only memory at address ${address:X4} with value {value}"
        );
    }

    public ushort Size => (ushort)_bytes.Length;
}
