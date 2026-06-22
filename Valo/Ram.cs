namespace Valo;

public sealed class Ram(byte[] bytes) : ISizedMemory
{
    public static LocatedMemory Located(uint start, uint end) =>
        new(start, end, new Ram(new byte[end - start]));

    public byte Read(ushort address) => bytes[address];

    public void Write(ushort address, byte value)
    {
        bytes[address] = value;
    }

    public ushort Size => (ushort)bytes.Length;
}
