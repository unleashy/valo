namespace Valo;

public class OpenBusMemory : IMemory
{
    public byte Read(ushort address) => 0xFF;

    public void Write(ushort address, byte value)
    {}
}
