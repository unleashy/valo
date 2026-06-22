using System.Collections.Immutable;

namespace Valo;

public sealed partial class MappedMemory(ImmutableArray<LocatedMemory> map) : IMemory
{
    private readonly ImmutableArray<LocatedMemory> _map =
        map.Sort((a, b) => (int)(a.Start - b.Start));

    public byte Read(ushort address)
    {
        var (memory, offset) = Map(address);
        return memory.Read(offset);
    }

    public void Write(ushort address, byte value)
    {
        var (memory, offset) = Map(address);
        memory.Write(offset, value);
    }

    private (IMemory, ushort offset) Map(ushort address)
    {
        if (_map.Length == 0) throw new UnmappedMemoryException(address);

        var len = _map.Length;
        var pivot = 0;

        while (len > 1) {
            var half = len / 2;
            var mid = pivot + half;

            if (_map[mid].Start <= address) {
                pivot = mid;
            }

            len -= half;
        }

        var match = _map[pivot];
        if (match.Start <= address && address < match.End) {
            return (match.Memory, (ushort)(address - match.Start));
        }

        throw new UnmappedMemoryException(address);
    }
}

public sealed class UnmappedMemoryException(ushort address) :
    ArgumentException($"Address ${address:X4} is not mapped");
