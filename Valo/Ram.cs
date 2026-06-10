namespace Valo;

public sealed class Ram(byte[] bytes) : ISizedMemory
{
    public byte Read(ushort address) => bytes[address];

    public void Write(ushort address, byte value)
    {
        bytes[address] = value;
    }

    public ushort Size => (ushort)bytes.Length;
}
