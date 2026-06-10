namespace Valo;

public interface IMemory
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
}

public interface ISizedMemory : IMemory
{
    ushort Size { get; }
}
