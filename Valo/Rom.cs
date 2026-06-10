using System.Collections.Immutable;

namespace Valo;

public sealed class Rom(ImmutableArray<byte> bytes) : ISizedMemory
{
    public byte Read(ushort address) => bytes[address];

    public void Write(ushort address, byte value)
    {
        throw new InvalidOperationException(
            $"Write to read-only memory at address ${address:X4} with value {value}"
        );
    }

    public ushort Size => (ushort)bytes.Length;
}
