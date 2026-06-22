using System.Diagnostics;

namespace Valo;

public sealed class AccessorMemory(Func<byte> read, Action<byte> write) : IMemory
{
    public static LocatedMemory Located(uint address, Func<byte> read, Action<byte> write) =>
        new(address, address + 1, new AccessorMemory(read, write));

    public byte Read(ushort address)
    {
        Debug.Assert(address == 0);
        return read();
    }

    public void Write(ushort address, byte value)
    {
        Debug.Assert(address == 0);
        write(value);
    }
}
