using System.Collections.Immutable;

namespace Valo;

public sealed partial class MappedMemory : IMemory
{
    private readonly record struct Allocation(uint Start, uint End, IMemory Memory);

    private readonly ImmutableArray<Allocation> _map;

    private MappedMemory(ImmutableArray<Allocation> map)
    {
        _map = map;
    }

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
        foreach (var alloc in _map) {
            if (alloc.Start <= address && address < alloc.End) {
                return (alloc.Memory, (ushort)(address - alloc.Start));
            }
        }

        throw new UnmappedMemoryException(address);
    }
}

public sealed class UnmappedMemoryException(ushort address) :
    ArgumentException($"Address ${address:X4} is not mapped");
