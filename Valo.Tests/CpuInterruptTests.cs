namespace Valo.Tests;

public class CpuInterruptTests : CpuTestsBase
{
    [TestCase(0b00001, 0b00001, 0x40, Description = "VBlank interrupt")]
    [TestCase(0b00010, 0b00010, 0x48, Description = "LCD interrupt")]
    [TestCase(0b00100, 0b00100, 0x50, Description = "Timer interrupt")]
    [TestCase(0b01000, 0b01000, 0x58, Description = "Serial interrupt")]
    [TestCase(0b10000, 0b10000, 0x60, Description = "Joypad interrupt")]
    public void Servicing(byte enabled, byte requested, byte vector)
    {
        const ushort pc = 0x1001;
        const ushort sp = 0x8002;
        const ushort ie = 0xFFFF;
        const ushort @if = 0xFF0F;

        var sut = new Cpu(
            new RegisterFile { IME = true, PC = pc - 1, SP = sp },
            new MappedMemory.Builder()
                .Map(pc - 1, new Rom([0x00, 0x00]))
                .Map(sp - 2, new Ram([0x00, 0x00]))
                .Map(vector, new Rom([0x00, 0x00]))
                .Map(ie, new Rom([enabled]))
                .Map(@if, new Ram([requested]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.IME, Is.False);
            Assert.That(sut.Registers.PC, Is.EqualTo(vector + 1));

            Assert.That(sut.Registers.SP, Is.EqualTo(sp - 2));
            Assert.That(sut.Memory.Read(sp - 1), Is.EqualTo(pc >> 8));
            Assert.That(sut.Memory.Read(sp - 2), Is.EqualTo(pc & 0xFF));

            Assert.That(sut.Memory.Read(@if), Is.EqualTo(requested & (requested - 1)));

            // 1 for NOP, 5 for servicing
            Assert.That(cycles, Is.EqualTo(1 + 5));
        });
    }
}
