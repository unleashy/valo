namespace Valo.Tests;

public class CpuInterruptTests : CpuTestsBase
{
    [TestCase(Interrupt.VBlank, Interrupt.VBlank, 0x40)]
    [TestCase(Interrupt.Lcd,    Interrupt.Lcd,    0x48)]
    [TestCase(Interrupt.Timer,  Interrupt.Timer,  0x50)]
    [TestCase(Interrupt.Serial, Interrupt.Serial, 0x58)]
    [TestCase(Interrupt.Joypad, Interrupt.Joypad, 0x60)]
    public void Servicing(Interrupt enabled, Interrupt requested, byte vector)
    {
        const ushort pc = 0x1001;
        const ushort sp = 0x8002;

        var sut = new Cpu(
            new RegisterFile { PC = pc - 1, SP = sp },
            new MappedMemory.Builder()
                .Map(pc - 1, new Rom([0x00, 0x00]))
                .Map(sp - 2, new Ram([0x00, 0x00]))
                .Map(vector, new Rom([0x00, 0x00]))
                .Build(),
            new InterruptController {
                MasterEnabled = true,
                Enabled = enabled,
                Requested = requested,
            }
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Interrupts.MasterEnabled, Is.False);
            Assert.That(sut.Registers.PC, Is.EqualTo(vector + 1));

            Assert.That(sut.Registers.SP, Is.EqualTo(sp - 2));
            Assert.That(sut.Memory.Read(sp - 1), Is.EqualTo(pc >> 8));
            Assert.That(sut.Memory.Read(sp - 2), Is.EqualTo(pc & 0xFF));

            Assert.That(sut.Interrupts.Requested, Is.EqualTo(requested & (requested - 1)));

            // 1 for NOP, 5 for servicing
            Assert.That(cycles, Is.EqualTo(1 + 5));
        });
    }
}
