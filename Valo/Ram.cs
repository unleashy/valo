namespace Valo;

public sealed class Ram(byte[] bytes) : IMemory
{
    public byte Read(ushort address) => bytes[address];

    public void Write(ushort address, byte value)
    {
        bytes[address] = value;
    }
}
