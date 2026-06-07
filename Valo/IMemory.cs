namespace Valo;

public interface IMemory
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
}
