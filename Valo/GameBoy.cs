namespace Valo;

public sealed class GameBoy(Cpu cpu, Ppu ppu)
{
    public const uint ClockHz = 4_194_304;
    public const ulong McyclesPerTcycles = 4;
    public const uint TcyclesPerFrame = 70_224;
    public const double MsPerFrame = (double)TcyclesPerFrame / ClockHz * 1000;

    private ulong _tCycles;

    public static GameBoy Create(Cartridge cartridge, ILcd lcd)
    {
        var vram  = Ram.Located(0x8000, 0xA000);
        var wram  = Ram.Located(0xC000, 0xDE00);
        var echo  = new LocatedMemory(0xE000, 0xFE00, wram.Memory);
        var oam   = Ram.Located(0xFE00, 0xFEA0);
        var hiram = Ram.Located(0xFF80, 0x10000);

        var ppu = new Ppu(lcd, vram.Memory, oam.Memory);

        var memory = new MappedMemory.Builder()
            .Map(vram, wram, echo, oam, hiram)
            .Map(cartridge.MemoryLayout())
            .Map(ppu.MemoryLayout())
            .Build();

        var cpu = new Cpu(new RegisterFile(), memory);
        BootMemory.Install(cpu);

        return new GameBoy(cpu, ppu);
    }

    public void EmulateSingleFrame()
    {
        Run(TcyclesPerFrame);
    }

    private void Run(ulong maxCycles)
    {
        var endCycles = _tCycles + maxCycles;
        while (_tCycles < endCycles) {
            Cycle();
        }
    }

    private void Cycle()
    {
        if (_tCycles % McyclesPerTcycles == 0) {
            cpu.Cycle();
        }

        ppu.Cycle();

        ++_tCycles;
    }
}
